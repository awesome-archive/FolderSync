//Project 2016 - Folder Sync v2
//Author: pandasxd (https://github.com/qhgz2013/FolderSync)
//
//commit.cs
//description: 对仓库/本地的提交进行修改，同时修改文件链接

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
        public void Commit_push(string local_addr, string title = "", string description = "", string root = "/", bool quiet = false)
        {
            if (string.IsNullOrEmpty(local_addr))
                return;
            local_addr = format_addr(Local_Replace(format_addr(local_addr)));
            root = format_addr(Local_Replace(format_addr(root)));
            //获取上一个提交和本地的文件列表
            List<local_file_data> local_file_list = Get_file_list(local_addr);
            local_file_list.Sort();
            List<commit_full_data> last_commit_file_list;
            if (_commit_list.Count > 0)
                last_commit_file_list = Get_commit_full_file_list(_commit_list[_commit_list.Count - 1].commit_SHA, root);
            else
                last_commit_file_list = new List<commit_full_data>();

            last_commit_file_list.Sort();

            //创建新的提交信息
            Random r = new Random();
            System.Security.Cryptography.SHA1CryptoServiceProvider sha_gen = new System.Security.Cryptography.SHA1CryptoServiceProvider();
            byte[] b_str_sha = sha_gen.ComputeHash(System.Text.Encoding.Default.GetBytes(local_addr + title + description + DateTime.Now.Ticks + "r" + r.NextDouble()));
            string str_sha = util.hex(b_str_sha).ToLower();

            //列表对比,产生差别文件清单
            #region list_diff

            int i_ptr = 0, j_ptr = 0;
            List<commit_delta_data> new_change_list = new List<commit_delta_data>();
            //对已有列表进行对比
            while (i_ptr < local_file_list.Count && j_ptr < last_commit_file_list.Count)
            {

                switch (string.Compare(local_file_list[i_ptr].path, last_commit_file_list[j_ptr].addr, true))
                {
                    case 1:
                        commit_delta_data data = new commit_delta_data(commit_operation.DEL, last_commit_file_list[j_ptr].addr, last_commit_file_list[j_ptr].MD5);

                        new_change_list.Add(data);
                        j_ptr++;
                        break;
                    case -1:
                        data = new commit_delta_data(commit_operation.ADD, root + local_file_list[i_ptr].path, local_file_list[i_ptr].MD5);

                        new_change_list.Add(data);
                        i_ptr++;
                        break;
                    case 0:
                        if (util.hex(local_file_list[i_ptr].MD5) != util.hex(last_commit_file_list[j_ptr].MD5))
                        {
                            data = new commit_delta_data(commit_operation.DEL, last_commit_file_list[j_ptr].addr, last_commit_file_list[j_ptr].MD5);
                            new_change_list.Add(data);

                            data = new commit_delta_data(commit_operation.ADD, root + local_file_list[i_ptr].path, local_file_list[i_ptr].MD5);
                            new_change_list.Add(data);
                        }
                        i_ptr++;
                        j_ptr++;
                        break;
                    default:
                        break;
                }
            }
            //删除多余的列表
            for(; j_ptr < last_commit_file_list.Count; j_ptr++)
            {
                commit_delta_data data = new commit_delta_data(commit_operation.DEL, last_commit_file_list[j_ptr].addr, last_commit_file_list[j_ptr].MD5);
                new_change_list.Add(data);
            }
            //添加到列表
            for (; i_ptr < local_file_list.Count; i_ptr++)
            {
                commit_delta_data data = new commit_delta_data(commit_operation.ADD, root + local_file_list[i_ptr].path, local_file_list[i_ptr].MD5);
                new_change_list.Add(data);
            }
            
            #endregion

            //提示确定
            if (!quiet)
                if (!(System.Windows.Forms.MessageBox.Show("本次提交将改变" + new_change_list.Count + "个文件，确定继续吗？", "确定", System.Windows.Forms.MessageBoxButtons.YesNoCancel) == System.Windows.Forms.DialogResult.Yes))
                    return;

            //保存提交总列表
            commit_list_data new_commit = new commit_list_data(b_str_sha, title, description, DateTime.Now.Ticks);
            _commit_list.Add(new_commit);
            if (_commit_list.Count > _max_commit_available)
            {
                Commit_delete(util.hex(_commit_list[0].commit_SHA).ToLower());
            }
            update_commit_list();
            update_global();

            //保存当前提交的文件列表
            save_commit_file(b_str_sha, new_change_list);

            //更改文件链接和复制列表
            foreach (commit_delta_data item in new_change_list)
            {
                switch (item.operation)
                {
                    case commit_operation.ADD:
                        create_link(item.MD5, item.addr, b_str_sha);
                        add_async_copy_to_repo(b_str_sha, item.MD5, item.addr, local_addr + item.addr.Substring(root.Length));
                        break;

                    case commit_operation.DEL:
                        break;

                    default:
                        break;
                }
            }

            update_ref_list();
            //保存完整文件列表
            //Get_commit_full_file_list(new_commit.commit_SHA);

        }
        public void Commit_pull(string local_addr, string commit_SHA = "", string root = "/", bool quiet = false)
        {
            if (string.IsNullOrEmpty(local_addr))
                return;
            byte[] b_commit_sha;
            if (string.IsNullOrEmpty(commit_SHA) && _commit_list.Count > 0)
                b_commit_sha = _commit_list.Last().commit_SHA;
            else if (string.IsNullOrEmpty(commit_SHA))
                return;
            else
                b_commit_sha = util.hex(commit_SHA);
            root = format_addr(Local_Replace(format_addr(root)));
            //load list
            List<local_file_data> local_ls = Get_file_list(local_addr);

            //load commit list
            List<commit_full_data> commit_ls = Get_commit_full_file_list(b_commit_sha);

            //列表对比,产生差别文件清单
            #region list_diff

            int i_ptr = 0, j_ptr = 0;
            List<async_file_data> new_change_list = new List<async_file_data>();
            //对已有列表进行对比
            while (i_ptr < local_ls.Count && j_ptr < commit_ls.Count)
            {

                switch (string.Compare(local_ls[i_ptr].path, commit_ls[j_ptr].addr, true))
                {
                    case 1:
                        async_file_data data = new async_file_data();
                        data.operation_type = async_operation.COPY_ORIGIN;
                        data.ref_file_md5 = commit_ls[j_ptr].MD5;
                        data.ref_file_addr = commit_ls[j_ptr].addr;
                        data.origin_addr = local_addr + commit_ls[j_ptr].addr.Substring(root.Length);
                        //add_async_copy_to_origin(commit_ls[j_ptr].MD5, commit_ls[j_ptr].addr, local_addr + commit_ls[j_ptr].addr.Substring(root.Length));
                        new_change_list.Add(data);
                        j_ptr++;
                        break;
                    case -1:
                        data = new async_file_data();
                        data.operation_type = async_operation.DELETE_ORIGIN;
                        data.origin_addr = local_addr + local_ls[i_ptr].path;
                        //add_async_delete_from_origin(local_addr + local_ls[i_ptr].path);

                        new_change_list.Add(data);
                        i_ptr++;
                        break;
                    case 0:
                        if (util.hex(local_ls[i_ptr].MD5) != util.hex(commit_ls[j_ptr].MD5))
                        {
                            data = new async_file_data();
                            data.origin_addr = local_addr + local_ls[i_ptr].path;
                            //add_async_delete_from_origin(local_addr + local_ls[i_ptr].path);
                            
                            new_change_list.Add(data);

                            data = new async_file_data();
                            data.operation_type = async_operation.COPY_ORIGIN;
                            data.ref_file_md5 = commit_ls[j_ptr].MD5;
                            data.ref_file_addr = commit_ls[j_ptr].addr;
                            data.origin_addr = local_addr + commit_ls[j_ptr].addr.Substring(root.Length);
                            //add_async_copy_to_origin(commit_ls[j_ptr].MD5, commit_ls[j_ptr].addr, local_addr + commit_ls[j_ptr].addr.Substring(root.Length));
                            new_change_list.Add(data);
                        }
                        i_ptr++;
                        j_ptr++;
                        break;
                    default:
                        break;
                }
            }
            
            for (; j_ptr < commit_ls.Count; j_ptr++)
            {
                async_file_data data = new async_file_data();
                data.operation_type = async_operation.COPY_ORIGIN;
                data.ref_file_md5 = commit_ls[j_ptr].MD5;
                data.ref_file_addr = commit_ls[j_ptr].addr;
                data.origin_addr = local_addr + commit_ls[j_ptr].addr.Substring(root.Length);
                //add_async_copy_to_origin(commit_ls[j_ptr].MD5, commit_ls[j_ptr].addr, local_addr + commit_ls[j_ptr].addr.Substring(root.Length));
                new_change_list.Add(data);
            }

            for (; i_ptr < local_ls.Count; i_ptr++)
            {
                async_file_data data = new async_file_data();
                data.operation_type = async_operation.DELETE_ORIGIN;
                data.origin_addr = local_addr + local_ls[i_ptr].path;
                //add_async_delete_from_origin(local_addr + local_ls[i_ptr].path);

                new_change_list.Add(data);
            }

            #endregion


            //提示确定
            if (!quiet)
                if (!(System.Windows.Forms.MessageBox.Show("本次提交将改变" + new_change_list.Count + "个文件，确定继续吗？", "确定", System.Windows.Forms.MessageBoxButtons.YesNoCancel) == System.Windows.Forms.DialogResult.Yes))
                    return;
            foreach (async_file_data item in new_change_list)
            {
                switch (item.operation_type)
                {
                    case async_operation.COPY_REPO:
                        break;
                    case async_operation.COPY_ORIGIN:
                        add_async_copy_to_origin(item.ref_file_md5, item.ref_file_addr, item.origin_addr);
                        break;
                    case async_operation.DELETE_REPO:
                        break;
                    case async_operation.DELETE_ORIGIN:
                        add_async_delete_from_origin(item.origin_addr);
                        break;
                    default:
                        break;
                }
            }
        }
        public void Commit_delete(string commit_SHA)
        {
            int index = Get_commit_index(commit_SHA);
            if (index == -1)
                return;
            //delete commit
            List<commit_delta_data> ls = Get_commit_file_list(index);
            foreach (commit_delta_data item in ls)
                if (item.operation == commit_operation.ADD)
                    remove_link(item.MD5, item.addr, util.hex(commit_SHA));
            foreach (commit_delta_data item in ls)
                if (can_delete_object(item.MD5))
                {
                    add_async_delete_from_repo(item.MD5);
                }
            File.Delete(_phys_addr + "\\commit\\" + commit_SHA);
            if (File.Exists(_phys_addr + "\\commit\\" + commit_SHA + "_full"))
                File.Delete(_phys_addr + "\\commit\\" + commit_SHA + "_full");

            _commit_list.RemoveAt(index);

            //and remove async list
            remove_async(util.hex(commit_SHA));

            //update
            update_global();
            update_commit_list();
            update_ref_list();
        }
        public void Commit_delete(int index)
        {
            Commit_delete(util.hex(Get_commit_data(index).commit_SHA).ToLower());
        }
        #region /commit/commitList
        //文件的提交清单
        public struct commit_list_data
        {
            public byte[] commit_SHA; //提交清单的离散值(全局唯一,为该提交的标识码)
            public string title; //提交清单的标题
            public string description; //提交清单的描述
            public long commit_tick; //提交的Tick
            public commit_list_data(byte[] commit_SHA, string title, string description = "", long commit_tick = 0)
            {
                this.commit_SHA = commit_SHA;
                this.title = title;
                this.description = description;
                if (commit_tick == 0)
                    this.commit_tick = DateTime.Now.Ticks;
                else
                    this.commit_tick = commit_tick;
            }
        }
        private List<commit_list_data> _commit_list;
        public int Commit_Count { get { return _commit_list.Count; } }
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
            return Get_commit_index(util.hex(commit_SHA));
        }
        public int Get_commit_index(byte[] commit_SHA)
        {
            if (commit_SHA == null)
                return -1;
            return _commit_list.FindIndex(commit => util.hex(commit.commit_SHA) == util.hex(commit_SHA));
        }
        public int Get_commit_index(commit_list_data data)
        {
            if (data.commit_SHA == null)
                return -1;
            return Get_commit_index(data.commit_SHA);
        }
        /// <summary>
        /// 获取前一个提交的离散值
        /// </summary>
        /// <param name="commit_SHA">当前提交的离散值</param>
        /// <returns></returns>
        public commit_list_data Get_former_commit(string commit_SHA)
        {
            return Get_former_commit(util.hex(commit_SHA));
        }
        public commit_list_data Get_former_commit(byte[] commit_SHA)
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
            return Get_later_commit(util.hex(commit_SHA));
        }
        public commit_list_data Get_later_commit(byte[] commit_SHA)
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
                FileStream _fs = new FileStream(target_file_name, FileMode.Create, FileAccess.Write);
                _fs.Write(buf, 0, 4);
                _fs.Close();
            }
            FileStream fs = new FileStream(target_file_name, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            //都是大同小异的简便数据读写方法，效率高，思路简单，代码简便。缺点也很明显，那就是文件结构一改起来就很麻烦
            uint list_size = br.ReadUInt32();
            for (int i = 0; i < list_size; i++)
            {
                commit_list_data data;
                data.commit_SHA = new byte[20];
                br.Read(data.commit_SHA, 0, 20);
                int str_len = br.ReadInt16();
                data.title = Encoding.Default.GetString(br.ReadBytes(str_len));
                str_len = br.ReadInt16();
                data.description = Encoding.Default.GetString(br.ReadBytes(str_len));
                data.commit_tick = br.ReadInt64();
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
                byte[] tmp = Encoding.Default.GetBytes(item.title);
                bw.Write((ushort)tmp.Length);
                bw.Write(tmp);
                tmp = Encoding.Default.GetBytes(item.description);
                bw.Write((ushort)tmp.Length);
                bw.Write(tmp);
                bw.Write(item.commit_tick);
            }

            bw.Close();
        }

        public List<commit_list_data> Get_commit_list()
        {
            return _commit_list.ToList();
        }
        #endregion

        #region /commit/[SHA delta commit]
        public struct commit_delta_data: IComparable<commit_delta_data>
        {
            public commit_operation operation;
            public string addr;
            public byte[] MD5;
            public commit_delta_data(commit_operation operation, string addr, byte[] MD5)
            {
                this.operation = operation;
                this.addr = addr;
                this.MD5 = MD5;
            }
            public int CompareTo(commit_delta_data data)
            {
                return addr.CompareTo(data.addr);
            }
        }
        public enum commit_operation
        {
            ADD,DEL //,FREEZE_ADD,FREEZE_DEL
        }
        public List<commit_delta_data> Get_commit_file_list(byte[] commit_SHA, string root= "/")
        {
            root = format_addr(Local_Replace(format_addr(root)));
            List<commit_delta_data> ret = new List<commit_delta_data>();
            if (commit_SHA == null)
                return ret;
            string target_file_addr = _phys_addr + "\\commit\\" + util.hex(commit_SHA).ToLower();
            if (!File.Exists(target_file_addr) || (new FileInfo(target_file_addr).Length < 4))
                return ret;

            FileStream fs = new FileStream(target_file_addr, FileMode.Open, FileAccess.Read);
            using (BinaryReader br = new BinaryReader(fs))
            {
                int size = br.ReadInt32();
                for (int i = 0; i < size; i++)
                {
                    commit_delta_data data = new commit_delta_data();
                    data.MD5 = new byte[16];
                    data.operation = (commit_operation) br.ReadByte();
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

                    if (data.addr.Substring(0, root.Length) == root)
                        ret.Add(data);
                }
            }
            return ret;
        }
        public List<commit_delta_data> Get_commit_file_list(string commit_SHA, string root="/")
        {
            return Get_commit_file_list(util.hex(commit_SHA), root);
        }
        public List<commit_delta_data> Get_commit_file_list(int index, string root = "/")
        {
            return Get_commit_file_list(Get_commit_data(index).commit_SHA, root);
        }
        private void save_commit_file(byte[] commit_SHA, List<commit_delta_data> new_list)
        {
            FileStream fs = new FileStream(_phys_addr + "\\commit\\" + util.hex(commit_SHA).ToLower(), FileMode.Create, FileAccess.Write);
            BinaryWriter bw = new BinaryWriter(fs);
            bw.Write(new_list.Count);

            foreach (commit_delta_data item in new_list)
            {
                //写入数据
                bw.Write((byte)item.operation);
                byte[] buf = System.Text.Encoding.Default.GetBytes(item.addr);
                bw.Write((short)buf.Length);
                bw.Write(buf);
                bw.Write(item.MD5);
                //将文件添加到同步列表
                //add_async(new_commit.commit_SHA, item.MD5, item.operation == commit_operation.ADD ? true : false, true, local_addr + item.addr);
            }

            bw.Close();

            Commit_full_file_list_update(commit_SHA);
        }
        private void Commit_full_file_list_update(byte[] commit_SHA)
        {
            int start_index = Get_commit_index(commit_SHA);
            if (start_index == -1)
                return;
            for (int i = start_index; i < _commit_list.Count; i++)
            {
                string file_name = _phys_addr + "\\commit\\" + util.hex(Get_commit_data(i).commit_SHA).ToLower() + "_full";
                if (File.Exists(file_name))
                    File.Delete(file_name);
            }
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

        public List<commit_full_data> Get_commit_full_file_list(byte[] commit_SHA, string root="/")
        {
            root = format_addr(Local_Replace(format_addr(root)));
            List<commit_full_data> ret = new List<commit_full_data>();
            SortedList<string, byte[]> storage = new SortedList<string, byte[]>();

            Stack<string> stack_to_load = new Stack<string>();

            if (commit_SHA == null)
                return ret;
            string str = util.hex(commit_SHA).ToLower();

            string target_file_name;

            while (true)
            {
                target_file_name = _phys_addr + "\\commit\\" + str.ToLower() + "_full";

                if (File.Exists(target_file_name) && (new FileInfo(target_file_name)).Length >= 4)
                {
                    FileStream fs = new FileStream(target_file_name, FileMode.Open, FileAccess.Read);
                    BinaryReader br = new BinaryReader(fs);
                    int len = br.ReadInt32();
                    for (int i = 0; i < len; i++)
                    {
                        int str_len = br.ReadInt16();
                        string addr = Encoding.Default.GetString(br.ReadBytes(str_len));
                        storage.Add(addr, br.ReadBytes(16));
                    }
                    br.Close();
                    break;
                }
                
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
                
                List<commit_delta_data> begin_list = Get_commit_file_list(str);
                foreach (commit_delta_data item in begin_list)
                {
                    switch (item.operation)
                    {
                        case commit_operation.ADD:
                            if (storage.ContainsKey(item.addr))
                                storage[item.addr] = item.MD5;
                            else
                                storage.Add(item.addr, item.MD5);
                            break;
                        case commit_operation.DEL:
                            if (storage.ContainsKey(item.addr))
                                storage.Remove(item.addr);
                            else
                                throw new InvalidOperationException("无法移除不存在的文件");
                            break;
                        default:
                            break;
                    }
                }
                
                //write to file
                FileStream fs2 = new FileStream(target_file_name, FileMode.Create, FileAccess.Write);
                BinaryWriter bw = new BinaryWriter(fs2);

                bw.Write(storage.Count);
                foreach (KeyValuePair<string,byte[]> item in storage)
                {
                    byte[] buf = Encoding.Default.GetBytes(item.Key);
                    bw.Write((short)buf.Length);
                    bw.Write(buf);
                    bw.Write(item.Value);
                }

                bw.Close();
            }

            foreach (KeyValuePair<string,byte[]> item in storage)
                if (item.Key.Substring(0, root.Length) == root)
                    ret.Add(new commit_full_data(item));

            return ret;
        }
        public List<commit_full_data> Get_commit_full_file_list(int index, string root="/")
        {
            return Get_commit_full_file_list(Get_commit_data(index).commit_SHA, root);
        }
        public List<commit_full_data> Get_commit_full_file_list(string commit_SHA,string root="/")
        {
            return Get_commit_full_file_list(util.hex(commit_SHA), root);
        }
        #endregion // /commit/[SHA full commit]
        #endregion // /commit
    }
}
