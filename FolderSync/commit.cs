using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FolderSync
{
    public partial class repo
    {
        #region commit
        public void Commit_push(string local_addr, string msg = "", string root = "/",/* bool force = false,*/ bool quiet = false)
        {
            local_addr = format_addr(Local_Replace(format_addr(local_addr)));
            root = format_addr(Local_Replace(format_addr(root)));
            //获取上一个提交和本地的文件列表
            List<local_file_data> local_file_list = Get_file_list(local_addr);
            local_file_list.Sort();
            List<commit_full_data> last_commit_file_list = Get_commit_full_file_list(util.hex(Get_former_commit(_front_commmit).commit_SHA), root);
            last_commit_file_list.Sort();

            //创建新的提交信息
            Random r = new Random();
            System.Security.Cryptography.SHA1CryptoServiceProvider sha_gen = new System.Security.Cryptography.SHA1CryptoServiceProvider();
            byte[] b_str_sha = sha_gen.ComputeHash(System.Text.Encoding.Default.GetBytes(local_addr + DateTime.Now.Ticks + "r" + r.NextDouble()));
            string str_sha = util.hex(b_str_sha).ToLower();

            //列表对比,产生差别文件清单
            #region list_diff

            int i_ptr = 0;
            List<commit_delta_data> new_change_list = new List<commit_delta_data>();
            commit_delta_data delta_data = new commit_delta_data();
            delta_data.MD5 = new byte[16];
            foreach (local_file_data item in local_file_list)
            {
                commit_full_data data = last_commit_file_list.ElementAtOrDefault(i_ptr);
                if (data.addr == null)
                {
                    delta_data.operation = COMMIT_OPERATION_ADD;
                    delta_data.addr = root + item.path;
                    delta_data.MD5 = item.MD5;
                    delta_data.status = FILE_UNSYNC;

                    new_change_list.Add(delta_data);
                    i_ptr++;
                }
                else
                {
                    while (i_ptr < last_commit_file_list.Count && string.Compare(last_commit_file_list.ElementAt(i_ptr).addr, root + item.path) < 0)
                    {
                        delta_data.operation = COMMIT_OPERATION_DEL;
                        delta_data.addr = root + item.path;
                        delta_data.MD5 = item.MD5;
                        delta_data.status = FILE_UNSYNC;
                        
                        new_change_list.Add(delta_data);
                        i_ptr++;
                    }

                    if (string.Compare(last_commit_file_list.ElementAt(i_ptr).addr, root + item.path) >= 0)
                    {
                        delta_data.operation = COMMIT_OPERATION_ADD;
                        delta_data.addr = root + item.path;
                        delta_data.MD5 = item.MD5;
                        delta_data.status = FILE_UNSYNC;
                        
                    new_change_list.Add(delta_data);
                    }
                }
            }


            #endregion

            //保存清单
            commit_list_data new_commit = new commit_list_data(b_str_sha, msg, local_addr, COMMIT_STAT_UNSYNC);
            _commit_list.Add(new_commit);
            _front_commmit = str_sha;
            if (_commit_list.Count > _max_commit_available)
            {
                _end_commit = util.hex(Get_commit_data(_commit_list.Count - _max_commit_available).commit_SHA).ToLower();
                for (int i = _commit_list.Count - _max_commit_available; i >= 0; i--)
                {
                    if ((_commit_list.ElementAt(i).stat & COMMIT_STAT_FREEZE) != 0)
                        break;
                    commit_list_data temp = _commit_list.ElementAt(i);
                    _commit_list.RemoveAt(i);
                    temp.stat = (temp.stat & ~COMMIT_STAT_FREEZE) & COMMIT_STAT_FREEZE;
                    _commit_list.Insert(i, temp);
                }
                //Commit_freeze(_end_commit)
            }
            update_commit_list();
            update_global();

            FileStream fs = new FileStream(_phys_addr + "\\commit\\" + str_sha, FileMode.Create, FileAccess.Write);
            BinaryWriter bw = new BinaryWriter(fs);
            bw.Write(new_change_list.Count);

            foreach (commit_delta_data item in new_change_list)
            {
                bw.Write(item.operation);
                byte[] buf = System.Text.Encoding.Default.GetBytes(item.addr);
                bw.Write((short)buf.Length);
                bw.Write(buf);
                bw.Write(item.MD5);
                bw.Write(item.status);
            }

            bw.Close();
            
            //创建线程以同步代码
            if (thd == null || thd.ThreadState == System.Threading.ThreadState.Stopped)
            {
                thd = new System.Threading.Thread(Sync_commit);
                thd.Name = "Data Sync Thread";
                thd.Start();
            }
        }
        System.Threading.Thread thd;

        public void Sync_commit()
        {
            for (int i = 0; i < _commit_list.Count; i++)
            {
                commit_list_data item = _commit_list[i];
                if ((item.stat & COMMIT_STAT_UNSYNC) != 0)
                {
                    string str_sha = util.hex(item.commit_SHA).ToLower();
                    List<commit_delta_data> file_list = Get_commit_file_list(str_sha);
                    if ((item.stat & COMMIT_STAT_FREEZE) != 0)
                    {
                        //TODO:
                        //sync freeze commit

                    }
                    else
                    {
                        //sync common commit
                        #region sync common commit
                        for (int j = 0; j < file_list.Count; j++)
                        {
                            if ((file_list[j].status & FILE_UNSYNC) != 0)
                            {
                                commit_delta_data tmp = file_list[j];
                                switch (tmp.operation)
                                {
                                    case COMMIT_OPERATION_ADD:
                                        file_copy(item.local_addr, tmp);
                                        break;
                                    case COMMIT_OPERATION_DEL:
                                        break;
                                    default:
                                        break;
                                }
                                tmp.status = (byte)(tmp.status & ~FILE_UNSYNC);
                                file_list[j] = tmp;
                            }
                        }
                        #endregion
                    }

                    #region update commit
                    FileStream fs = new FileStream(_phys_addr + "\\commit\\" + str_sha, FileMode.Create, FileAccess.Write);
                    BinaryWriter bw = new BinaryWriter(fs);
                    bw.Write(file_list.Count);

                    foreach (commit_delta_data item2 in file_list)
                    {
                        bw.Write(item2.operation);
                        byte[] buf = System.Text.Encoding.Default.GetBytes(item2.addr);
                        bw.Write((short)buf.Length);
                        bw.Write(buf);
                        bw.Write(item2.MD5);
                        bw.Write(item2.status);
                    }

                    bw.Close();
                    #endregion

                    item.stat = item.stat & ~COMMIT_STAT_UNSYNC;
                    _commit_list[i] = item;
                    update_commit_list();
                }
            }
        }
        private void file_copy(string local_addr, commit_delta_data data)
        {
            byte[] buf = new byte[1048576];
            string src_name = local_addr + data.addr;
            string str_md5 = util.hex(data.MD5).ToLower();
            string dst_name = _phys_addr + "\\object\\" + str_md5.Substring(0, 2) + "\\" + str_md5;

            if (File.Exists(dst_name))
                return;
            FileStream f_in = new FileStream(src_name, FileMode.Open, FileAccess.Read);
            FileStream f_out = new FileStream(dst_name, FileMode.Create, FileAccess.Write);

            long pos = 0;
            int read = 0;
            do
            {
                read = f_in.Read(buf, 0, 1048576);
                pos += read;
                f_out.Write(buf, 0, read);

                if (CopyStatusUpdate != null)
                    CopyStatusUpdate(src_name, pos, f_in.Length);
            } while (read != 0);

            f_in.Close();
            f_out.Close();

            FileInfo fi_in = new FileInfo(src_name);
            FileInfo fi_out = new FileInfo(dst_name);

            fi_out.LastWriteTime = fi_in.LastWriteTime;
        }
        public delegate void CopyStatusHandler(string abs_path, long pos, long len);
        public event CopyStatusHandler CopyStatusUpdate;
        private void file_delete(string local_addr, commit_delta_data data)
        {
            string str_md5 = util.hex(data.MD5).ToLower();
            string dst_name = _phys_addr + "\\" + str_md5.Substring(0, 2) + "\\" + str_md5;
            File.Delete(dst_name);
        }
        #region /commit/commitList
        //文件的提交清单
        public struct commit_list_data
        {
            public byte[] commit_SHA; //提交清单的离散值(全局唯一,为该提交的标识码)
            public string description; //提交清单的描述
            public string local_addr; //提交清单的根地址，用于同步用
            public int stat; //状态
            public commit_list_data(byte[] commit_SHA, string description, string local_addr, int stat)
            {
                this.commit_SHA = commit_SHA;
                this.description = description;
                this.local_addr = local_addr;
                this.stat = stat;
            }
        }
        public const int COMMIT_STAT_FREEZE = 1;
        public const int COMMIT_STAT_UNSYNC = 2;
        public const int COMMIT_STAT_RESERVED = 0xffffffc;
        private List<commit_list_data> _commit_list;
        /// <summary>
        /// 获取提交清单中指定下标的离散值
        /// </summary>
        /// <param name="index">下标</param>
        /// <returns></returns>
        public commit_list_data Get_commit_data(int index)
        {
            if (index < 0 || _commit_list.Count <= index)
                //throw new IndexOutOfRangeException("获取提交SHA值时下标越界， index=" + index.ToString());
                return new commit_list_data();
            return _commit_list.ElementAt(index);
        }
        public commit_list_data Get_commit_data(commit_list_data data)
        {
            return Get_commit_data(Get_commit_index(data));
        }
        /// <summary>
        /// 获取指定离散值的下标
        /// </summary>
        /// <param name="commit_SHA">提交标识码</param>
        /// <returns>成功:>=0,失败:-1</returns>
        public int Get_commit_index(string commit_SHA)
        {
            if (string.IsNullOrEmpty(commit_SHA))
                return -1;
            byte[] tmp = util.hex(commit_SHA);
            return _commit_list.FindIndex(commit => commit.commit_SHA == tmp);
        }
        public int Get_commit_index(commit_list_data data)
        {
            if (data.commit_SHA == null)
                return -1;
            return Get_commit_index(util.hex(data.commit_SHA));
        }
        /// <summary>
        /// 获取前一个提交的离散值
        /// </summary>
        /// <param name="commit_SHA">当前提交的离散值</param>
        /// <returns></returns>
        public commit_list_data Get_former_commit(string commit_SHA)
        {
            int index = Get_commit_index(commit_SHA);
            if (index <= 0) //0 or -1
                return new commit_list_data();
            else
                return Get_commit_data(Get_commit_index(commit_SHA) - 1);
        }
        public commit_list_data Get_former_commit(commit_list_data commit)
        {
            return Get_former_commit(util.hex(commit.commit_SHA));
        }
        /// <summary>
        /// 获取下一个提交的离散值
        /// </summary>
        /// <param name="commit_SHA">当前提交的离散值</param>
        /// <returns></returns>
        public commit_list_data Get_later_commit(string commit_SHA)
        {
            int index = Get_commit_index(commit_SHA);
            if (index == _commit_list.Count || index == -1)
                return new commit_list_data();
            else
                return Get_commit_data(Get_commit_index(commit_SHA) + 1);
        }
        public commit_list_data Get_later_commit(commit_list_data commit)
        {
            return Get_later_commit(util.hex(commit.commit_SHA));
        }
        //初始化仓库的提交列表，注意要将此函数列入到构造函数中执行
        private void init_commit_list()
        {
            _commit_list = new List<commit_list_data>();
            string target_file_name = _phys_addr + "\\commit\\commitList";
            //文件不存在时自动创建,一般来说不会,除非是人工作死删除
            if (!File.Exists(target_file_name) || (new FileInfo(target_file_name)).Length < 4)
            {
                byte[] buf = new byte[4];
                FileStream _fs = new FileStream(target_file_name, FileMode.CreateNew, FileAccess.Write);
                _fs.Write(buf, 0, 4);
                _fs.Close();
            }
            FileStream fs = new FileStream(target_file_name, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            //都是大同小异的简便数据读写方法，效率高，思路简单，代码简便。缺点也很明显，那就是文件结构一改起来就很麻烦
            uint list_size = br.ReadUInt32();
            commit_list_data data;
            data.commit_SHA = new byte[20];
            for (int i = 0; i < list_size; i++)
            {
                br.Read(data.commit_SHA, 0, 20);
                ushort str_len = br.ReadUInt16();
                if (str_len > 0)
                {
                    byte[] tmp = new byte[str_len];
                    br.Read(tmp, 0, str_len);
                    data.description = Encoding.Default.GetString(tmp);
                }
                else
                    data.description = string.Empty;
                str_len = br.ReadUInt16();
                if (str_len > 0)
                {
                    byte[] tmp = new byte[str_len];
                    br.Read(tmp, 0, str_len);
                    data.local_addr = Encoding.Default.GetString(tmp);
                }
                else
                    data.local_addr = "";
                data.stat = br.ReadInt32();
                _commit_list.Add(data);
            }

            br.Close();
        }
        //更新提交列表,对提交的修改后要执行此函数进行保存
        private void update_commit_list()
        {
            FileStream fs = new FileStream(_phys_addr + "\\commit\\commitList", FileMode.Create, FileAccess.Write);
            BinaryWriter bw = new BinaryWriter(fs);

            bw.Write(_commit_list.Count);

            foreach (commit_list_data item in _commit_list)
            {
                bw.Write(item.commit_SHA);
                byte[] tmp = Encoding.Default.GetBytes(item.description);
                bw.Write((ushort)tmp.Length);
                bw.Write(tmp);
                tmp = Encoding.Default.GetBytes(item.local_addr);
                bw.Write((ushort)tmp.Length);
                bw.Write(tmp);
                bw.Write(item.stat);
            }

            bw.Close();
        }
        #endregion

        #region /commit/[SHA delta commit]
        public struct commit_delta_data: IComparable<commit_delta_data>
        {
            public byte operation;
            public string addr;
            public byte[] MD5;
            public byte status;
            public commit_delta_data(byte operation, string addr, byte[] MD5,byte status)
            {
                this.operation = operation;
                this.addr = addr;
                this.MD5 = MD5;
                this.status = status;
            }
            public int CompareTo(commit_delta_data data)
            {
                return addr.CompareTo(data.addr);
            }
        }
        public const byte COMMIT_OPERATION_ADD = 1;
        public const byte COMMIT_OPERATION_DEL = 2;
        public const byte FILE_UNSYNC = 1;
        public List<commit_delta_data> Get_commit_file_list(string commit_SHA, string root= "/")
        {
            root = format_addr(Local_Replace(format_addr(root)));
            List<commit_delta_data> ret = new List<commit_delta_data>();
            if (string.IsNullOrEmpty(commit_SHA))
                return ret;
            if (!File.Exists(_phys_addr + "\\commit\\" + commit_SHA.ToLower()) || (new FileInfo(_phys_addr + "\\commit\\" + commit_SHA.ToLower()).Length < 4))
                return ret;

            FileStream fs = new FileStream(_phys_addr + "\\commit\\" + commit_SHA.ToLower(), FileMode.Open, FileAccess.Read);
            using (BinaryReader br = new BinaryReader(fs))
            {
                int size = br.ReadInt32();
                for (int i = 0; i < size; i++)
                {
                    commit_delta_data data = new commit_delta_data();
                    data.MD5 = new byte[16];
                    data.operation = br.ReadByte();
                    int str_len = br.ReadInt16();
                    if (str_len > 0)
                    {
                        byte[] buf = new byte[str_len];
                        br.Read(buf, 0, str_len);
                        data.addr = System.Text.Encoding.Default.GetString(buf);
                    }
                    else
                        data.addr = "";
                    br.Read(data.MD5, 0, 16);
                    data.status = br.ReadByte();

                    if (data.addr.Substring(0, root.Length) == root)
                        ret.Add(data);
                }
            }
            return ret;
        }
        public List<commit_delta_data> Get_commit_file_list(int index, string root = "/")
        {
            return Get_commit_file_list(util.hex(Get_commit_data(index).commit_SHA), root);
        }
        #endregion // /commit/[SHA delta commit]

        #region /commit/[SHA full commit]
        public struct commit_full_data: IComparable<commit_full_data>
        {
            public string addr;
            public byte[] MD5;
            public int CompareTo(commit_full_data data)
            {
                return addr.CompareTo(data.addr);
            }
            public commit_full_data(string addr, byte[] MD5)
            {
                this.addr = addr;
                this.MD5 = MD5;
            }
            public commit_full_data(KeyValuePair<string,byte[]> data)
            {
                addr = data.Key;
                MD5 = data.Value;
            }
        }

        public List<commit_full_data> Get_commit_full_file_list(string commit_SHA, string root="/")
        {
            root = format_addr(Local_Replace(format_addr(root)));
            List<commit_full_data> ret = new List<commit_full_data>();
            SortedList<string, byte[]> storage = new SortedList<string, byte[]>();

            Stack<string> stack_to_load = new Stack<string>();
            string str = commit_SHA;

            string target_file_name;

            while (true)
            {
                target_file_name = _phys_addr + "\\commit\\" + str.ToLower() + "_full";
                if (File.Exists(target_file_name) && (new FileInfo(target_file_name)).Length >= 4)
                    break;

                stack_to_load.Push(str);

                commit_list_data last_data = Get_former_commit(str);
                if (last_data.commit_SHA == null)
                    break;

                str = util.hex(last_data.commit_SHA);
            }

            while (stack_to_load.Count > 0)
            {
                str = stack_to_load.Pop();
                commit_list_data last_commit = Get_former_commit(str);
                target_file_name = _phys_addr + "\\commit\\" + str.ToLower() + "_full";

                List<commit_delta_data> last_delta_file = Get_commit_file_list(util.hex(last_commit.commit_SHA));
                foreach (commit_delta_data item in last_delta_file)
                {
                    if (storage.ContainsKey(item.addr))
                        storage[item.addr] = item.MD5;
                    else
                        storage.Add(item.addr, item.MD5);
                }
            }

            foreach (KeyValuePair<string,byte[]> item in storage)
                if (item.Key.Substring(0, root.Length) == root)
                    ret.Add(new commit_full_data(item));

            return ret;
        }
        public List<commit_full_data> Get_commit_full_file_list(int index, string root="/")
        {
            return Get_commit_full_file_list(util.hex(Get_commit_data(index).commit_SHA), root);
        }
        #endregion // /commit/[SHA full commit]
        #endregion // /commit
    }
}
