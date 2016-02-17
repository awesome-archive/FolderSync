//Project 2016 - Folder Sync v2
//Author: pandasxd (https://github.com/qhgz2013/FolderSync)
//
//async_copy.cs
//description: 用于文件的异步/断点读写

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace FolderSync
{
    public partial class repo
    {
        #region Async Operation
        private struct async_file_data
        {
            //字段使用情况: +R : to repo, -R: delete repo, +O: to origin, -O: delete origin
            public async_operation operation_type; //+R -R +O -O
            public byte[] ref_file_md5;            //+R -R +O

            public byte[] ref_commit_sha;          //+R
            public string ref_file_addr;           //+R    +O

            public string origin_addr;             //+R    +O -O

            public long origin_modify_time;        //+R    +O
            public long pos;                       //+R    +O
            public long length;                    //+R    +O

        }
        private enum async_operation
        {
            COPY_REPO, COPY_ORIGIN, DELETE_REPO, DELETE_ORIGIN
        }
        // 由于IO的单线程读写速度远远大于多线程，所以只设一条异步线程
        private Thread _async_thread;
        private bool _async_thread_stop_flag;
        private bool _async_thread_init_flag;
        // [warning] : 多线程读写安全
        private ConcurrentQueue<async_file_data> _async_file_list;
        private void _async_thread_callback()
        {
            _async_thread_init_flag = true;
            _async_thread_stop_flag = false;

            const int default_buffer_size = 0x100000;
            byte[] buffer = new byte[default_buffer_size];

            while (!_async_thread_stop_flag)
            {
                while (!_async_thread_stop_flag)
                {
                    if (_async_file_list.Count <= 0)
                        break;

                    async_file_data data;
                    _async_file_list.TryDequeue(out data);
                        
                    
                    #region r1
                    
                    switch (data.operation_type)
                    {

                        #region origin -> repo
                        case async_operation.COPY_REPO:
                            // origin -> repo
                            string dst_file_name = _phys_addr + "\\object\\" + util.hex(data.ref_file_md5[0]).ToLower() + "\\" + util.hex(data.ref_file_md5).ToLower();

                            FileInfo fi = new FileInfo(data.origin_addr); //->origin
                            FileInfo fi2 = new FileInfo(dst_file_name); //->repo
                            if (fi.Length != data.length || fi.LastWriteTime.Ticks != data.origin_modify_time)
                            {
                                //file change, update to this commit

                                //WARN: MULTITHREAD SAFE
                                List<commit_delta_data> _this_commit_list = Get_commit_file_list(data.ref_commit_sha);

                                //todo: fast-forward list search
                                int index = _this_commit_list.FindIndex(element => element.addr == data.ref_file_addr);
                                commit_delta_data changed_data = _this_commit_list[index];
                                changed_data.MD5 = calculate_md5(data.origin_addr);
                                _this_commit_list[index] = changed_data;

                                //! todo: safe invoke
                                save_commit_file(data.ref_commit_sha, _this_commit_list);

                                //LOG
                                log_msg(LogType.WARN, "文件 " + data.origin_addr + " 已更改，已在提交清单上修正");
                            }
                            if (fi2.Exists && fi2.Length != data.pos)
                            {
                                data.pos = 0;
                            }

                            FileStream fs_in = new FileStream(data.origin_addr, FileMode.Open, FileAccess.Read);
                            FileStream fs_out;
                            if (data.pos > 0)
                                fs_out = new FileStream(dst_file_name, FileMode.Append, FileAccess.Write);
                            else
                                fs_out = new FileStream(dst_file_name, FileMode.Create, FileAccess.Write);

                            int cur_read = 0;
                            fs_in.Seek(data.pos, SeekOrigin.Begin);

                            do
                            {
                                cur_read = fs_in.Read(buffer, 0, default_buffer_size);
                                fs_out.Write(buffer, 0, cur_read);
                                data.pos += cur_read;

                                if (Copy_Status != null)
                                    Copy_Status(data.origin_addr, data.pos, data.length);

                            } while (cur_read != 0);

                            fs_in.Close();
                            fs_out.Close();

                            break;

                        #endregion

                        #region repo -> origin
                        case async_operation.COPY_ORIGIN:
                            // repo -> origin
                            dst_file_name = _phys_addr + "\\object\\" + util.hex(data.ref_file_md5[0]).ToLower() + "\\" + util.hex(data.ref_file_md5).ToLower();

                            fi2 = new FileInfo(data.origin_addr); //->origin
                            fi = new FileInfo(dst_file_name); //->repo
                            if (fi2.Exists && fi2.Length != data.pos)
                            {
                                data.pos = 0;
                            }

                            System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(@"(?<path>.*)\\(?<name>.*?)");
                            string dir = reg.Match(data.origin_addr).Result("${path}");
                            if (!Directory.Exists(dir))
                                Directory.CreateDirectory(dir);

                            fs_in = new FileStream(dst_file_name, FileMode.Open, FileAccess.Read);
                            
                            if (data.pos > 0)
                                fs_out = new FileStream(data.origin_addr, FileMode.Append, FileAccess.Write);
                            else
                                fs_out = new FileStream(data.origin_addr, FileMode.Create, FileAccess.Write);

                            cur_read = 0;
                            fs_in.Seek(data.pos, SeekOrigin.Begin);

                            do
                            {
                                cur_read = fs_in.Read(buffer, 0, default_buffer_size);
                                fs_out.Write(buffer, 0, cur_read);
                                data.pos += cur_read;

                                if (Copy_Status != null)
                                    Copy_Status(data.origin_addr, data.pos, data.length);

                            } while (cur_read != 0);

                            fs_in.Close();
                            fs_out.Close();

                            break;

                        #endregion

                        #region delete operation
                        case async_operation.DELETE_REPO:
                            File.Delete(_phys_addr + "\\object\\" + util.hex(data.ref_file_md5[0]).ToLower() + "\\" + util.hex(data.ref_file_md5).ToLower());
                            break;

                        case async_operation.DELETE_ORIGIN:
                            if (File.Exists(data.origin_addr))
                                File.Delete(data.origin_addr);
                            else if (Directory.Exists(data.origin_addr))
                                Directory.Delete(data.origin_addr, true);

                            break;

                        #endregion
                        default:
                            throw new InvalidCastException("操作类型错误:" + data.operation_type);
                    }
                    #endregion
                }
                Thread.Sleep(1000);
            }

            update_async();

            _async_thread_init_flag = false;
        }

        /*
         * ******  **    **  ******  **   **  ******
         * **      **    **  **      ***  **    **
         * ******  **    **  ******  ** * **    **
         * **       **  **   **      **  ***    **
         * ******     **     ******  **   **    **
         */
        public delegate void Copy_Status_Handler(string addr, long pos, long len);
        public event Copy_Status_Handler Copy_Status;
        /// <summary>
        /// 增加到同步列表
        /// </summary>
        /// <param name="commit_sha">提交离散值</param>
        /// <param name="md5">文件md5</param>
        /// <param name="file_addr">文件在仓库的位置</param>
        /// <param name="is_copy_mode">是否为复制(true:复制,false:删除)</param>
        /// <param name="is_to_repo">是否为仓库的操作(true:对仓库进行操作,false:对原文件进行操作)</param>
        /// <param name="origin_addr">原文件地址(可以是目录)</param>

        private void add_async_copy_to_repo(byte[] commit_sha, byte[] md5, string repo_addr, string origin_addr)
        {
            async_file_data data = new async_file_data();
            data.ref_commit_sha = commit_sha;
            data.ref_file_md5 = md5;
            data.ref_file_addr = repo_addr;
            data.origin_addr = origin_addr;

            FileInfo fi = new FileInfo(origin_addr);
            data.pos = 0;
            data.length = fi.Length;
            data.origin_modify_time = fi.LastWriteTime.Ticks;
            data.operation_type = async_operation.COPY_REPO;

            _async_file_list.Enqueue(data);
        }
        private void add_async_delete_from_repo(byte[] md5)
        {
            async_file_data data = new async_file_data();
            data.ref_file_md5 = md5;

            data.operation_type = async_operation.DELETE_REPO;

            _async_file_list.Enqueue(data);
        }
        private void add_async_copy_to_origin(byte[] md5, string repo_addr, string origin_addr)
        {
            async_file_data data = new async_file_data();
            data.ref_file_md5 = md5;
            data.ref_file_addr = origin_addr;

            data.origin_addr = origin_addr;
            FileInfo fi = new FileInfo(_phys_addr + "\\object\\" + util.hex(md5[0]).ToLower() + "\\" + util.hex(md5).ToLower());
            data.pos = 0;
            data.length = fi.Length;
            data.origin_modify_time = fi.LastWriteTime.Ticks;
            data.operation_type = async_operation.COPY_ORIGIN;

            _async_file_list.Enqueue(data);
        }
        private void add_async_delete_from_origin(string origin_addr)
        {
            async_file_data data = new async_file_data();
            data.origin_addr = origin_addr;
            data.operation_type = async_operation.DELETE_ORIGIN;

            _async_file_list.Enqueue(data);
        }
        private void remove_async(byte[] commit_sha)
        {
            string str_sha = util.hex(commit_sha);
            /*
            for (int i = 0; i < _async_file_list.Count; i++)
            {
                if (util.hex(_async_file_list[i].ref_commit_sha) == str_sha)
                {
                    lock (((ICollection)_async_file_list).SyncRoot)
                    {
                        _async_file_list.RemoveAt(i--);
                    }
                }
            }
            */
        }
        private void init_async()
        {
            if (!_async_thread_init_flag)
            {
                _async_thread = new Thread(_async_thread_callback);
                _async_thread.Name = "File Async Copy Thread";
            }

            _async_file_list = new ConcurrentQueue<async_file_data>();

            string target_file_name = _phys_addr + "\\ASYNC_LIST";

            if (!File.Exists(target_file_name) || (new FileInfo(target_file_name).Length < 4))
            {
                FileStream fs0 = new FileStream(target_file_name, FileMode.Create, FileAccess.Write);
                fs0.Write(new byte[] { 0, 0, 0, 0 }, 0, 4);
                fs0.Close();
            }

            FileStream fs = new FileStream(target_file_name, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);

            int len = br.ReadInt32();
            for (int i = 0; i < len; i++)
            {
                async_file_data data = new async_file_data();

                int str_len;

                data.operation_type = (async_operation)br.ReadByte();

                switch (data.operation_type)
                {
                    case async_operation.COPY_REPO:
                        data.ref_file_md5 = br.ReadBytes(16);
                        data.ref_commit_sha = br.ReadBytes(20);
                        str_len = br.ReadInt16();
                        data.ref_file_addr = Encoding.Default.GetString(br.ReadBytes(str_len));
                        str_len = br.ReadInt16();
                        data.origin_addr = Encoding.Default.GetString(br.ReadBytes(str_len));
                        data.origin_modify_time = br.ReadInt64();
                        data.pos = br.ReadInt64();
                        data.length = br.ReadInt64();
                        break;
                    case async_operation.COPY_ORIGIN:
                        data.ref_file_md5 = br.ReadBytes(16);
                        str_len = br.ReadInt16();
                        data.ref_file_addr = Encoding.Default.GetString(br.ReadBytes(str_len));
                        str_len = br.ReadInt16();
                        data.origin_addr = Encoding.Default.GetString(br.ReadBytes(str_len));
                        data.origin_modify_time = br.ReadInt64();
                        data.pos = br.ReadInt64();
                        data.length = br.ReadInt64();
                        break;
                    case async_operation.DELETE_REPO:
                        data.ref_file_md5 = br.ReadBytes(16);
                        break;
                    case async_operation.DELETE_ORIGIN:
                        str_len = br.ReadInt16();
                        data.origin_addr = Encoding.Default.GetString(br.ReadBytes(str_len));
                        break;
                    default:
                        throw new InvalidCastException("操作类型错误:" + data.operation_type);
                }
                _async_file_list.Enqueue(data);
            }

            br.Close();

            if (!_async_thread_init_flag)
                _async_thread.Start();
        }
        private void update_async()
        {
            string target_file_name = _phys_addr + "\\ASYNC_LIST";
            FileStream fs = new FileStream(target_file_name, FileMode.Create, FileAccess.Write);
            BinaryWriter bw = new BinaryWriter(fs);

            bw.Write(_async_file_list.Count);
            foreach (async_file_data item in _async_file_list)
            {
                byte[] buf = Encoding.Default.GetBytes(item.ref_file_addr);
                bw.Write((short)buf.Length);
                bw.Write(buf);

                bw.Write((byte)item.operation_type);

                switch (item.operation_type)
                {
                    case async_operation.COPY_REPO:
                        bw.Write(item.ref_file_md5);
                        bw.Write(item.ref_commit_sha);
                        buf = Encoding.Default.GetBytes(item.ref_file_addr);
                        bw.Write((short)buf.Length);
                        bw.Write(buf);
                        buf = Encoding.Default.GetBytes(item.origin_addr);
                        bw.Write((short)buf.Length);
                        bw.Write(buf);
                        bw.Write(item.origin_modify_time);
                        bw.Write(item.pos);
                        bw.Write(item.length);
                        break;
                    case async_operation.COPY_ORIGIN:
                        bw.Write(item.ref_file_md5);
                        buf = Encoding.Default.GetBytes(item.ref_file_addr);
                        bw.Write((short)buf.Length);
                        bw.Write(buf);
                        buf = Encoding.Default.GetBytes(item.origin_addr);
                        bw.Write((short)buf.Length);
                        bw.Write(buf);
                        bw.Write(item.origin_modify_time);
                        bw.Write(item.pos);
                        bw.Write(item.length);
                        break;
                    case async_operation.DELETE_REPO:
                        bw.Write(item.ref_file_md5);
                        break;
                    case async_operation.DELETE_ORIGIN:
                        buf = Encoding.Default.GetBytes(item.origin_addr);
                        bw.Write((short)buf.Length);
                        bw.Write(buf);
                        break;
                    default:
                        throw new InvalidCastException("操作类型错误:" + item.operation_type);
                }
            }

            bw.Close();
        }
        #endregion //Async Operation
    }
}
