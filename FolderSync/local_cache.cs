using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FolderSync
{
    public partial class repo
    {
        //缓存文件夹所有文件的MD5值
        #region /local manager
        //缓存文件的数据结构
        public struct local_file_data : IComparable<local_file_data>
	    {
            public string path; //相对路径
            public byte[] MD5;
            public DateTime modify_time; //文件最后修改时间
            public long length;
            public int CompareTo(local_file_data data)
            {
                return path.CompareTo(data.path);
            }
            public local_file_data(string path, byte[] MD5, DateTime modify_time, long length)
            {
                this.path = path;
                this.MD5 = MD5;
                this.modify_time = modify_time;
                this.length = length;
            }
	    }
        /// <summary>
        /// 将绝对路径转换为相对路径
        /// </summary>
        /// <param name="root">根路径</param>
        /// <param name="file">绝对文件路径</param>
        /// <returns></returns>
        private string relative_addr(string root,string file)
        {
            return file.Replace(root, "");
        }
        /// <summary>
        /// 将相对路径转换为绝对路径
        /// </summary>
        /// <param name="root">根路径</param>
        /// <param name="file">相对文件路径</param>
        /// <returns></returns>
        private string absolute_addr(string root,string file)
        {
            if (file[0] == '\\')
                return root + file;
            else
                return root + "\\" + file;
        }
        /// <summary>
        /// 获取文件夹及其子文件夹的所有文件地址
        /// </summary>
        /// <param name="path">文件夹地址</param>
        /// <returns></returns>
        private List<string> get_all_file(string path)
        {
            List<string> ret = new List<string>();
            //路径不存在返回空列表
            if (!Directory.Exists(path))
                return ret;
            //遍历该文件夹内的所有文件
            foreach (string file_name in Directory.GetFiles(path))
                ret.Add(file_name);
            //递归读取子文件夹的文件列表
            foreach (string sub_path in Directory.GetDirectories(path))
                ret.AddRange(get_all_file(sub_path));
            return ret;
        }
        /// <summary>
        /// 读取指定文件夹的文件地址和修改时间并自动缓存
        /// </summary>
        /// <param name="local_addr">文件夹地址</param>
        /// <returns></returns>
        public List<local_file_data> Get_file_list(string local_addr)
        {
            List<local_file_data> ret = new List<local_file_data>();
            //将路径统一替换处理
            local_addr = format_addr(Local_Replace(format_addr(local_addr.ToLower())));

            log_msg(LogType.DEBUG, "get file list in " + local_addr);

            System.Security.Cryptography.SHA1CryptoServiceProvider sha = new System.Security.Cryptography.SHA1CryptoServiceProvider();
            //计算路径的离散值,用于保存缓存文件
            string local_addr_sha = util.hex(sha.ComputeHash(System.Text.Encoding.Default.GetBytes(local_addr))).ToLower();
            string target_file_name = _phys_addr + "\\local\\" + local_addr_sha;
            //缓存文件不存在时创建
            if ((!File.Exists(target_file_name)) || (new FileInfo(target_file_name)).Length < 4)
            {
                using (FileStream _fs = new FileStream(target_file_name, FileMode.Create, FileAccess.ReadWrite))
                {
                    byte[] buf = new byte[4];
                    _fs.Write(buf, 0, 4);
                }
            }

            //读取缓存文件
            //文件结构很简单(所有数字类型的低位在上!),写入也同理
            //首先4B int类型的数据个数
            //然后就是循环的: 2B int的字符串字节数,后面跟指定的字符串
            //16B byte[]的文件MD5值
            //8B long的文件修改时间(Tick为单位)
            FileStream fs = new FileStream(target_file_name, FileMode.Open, FileAccess.ReadWrite);
            using (BinaryReader br = new BinaryReader(fs))
            {
                int len = br.ReadInt32();
                for (int i = 0; i < len; i++)
                {
                    local_file_data data = new local_file_data();
                    data.MD5 = new byte[16];
                    int str_len = br.ReadInt16();
                    if (str_len > 0)
                    {
                        byte[] tmp = new byte[str_len];
                        br.Read(tmp, 0, str_len);
                        data.path = System.Text.Encoding.Default.GetString(tmp);
                    }
                    else
                        data.path = "";
                    br.Read(data.MD5, 0, 16);
                    data.modify_time = new DateTime(br.ReadInt64());
                    data.length = br.ReadInt64();
                    ret.Add(data);
                } //for


            } //using

            log_msg(LogType.DEBUG, "local cache has " + ret.Count + " file data");

            int i_ptr = 0, j_ptr = 0;
            //下标比较,前提：要排序
            List<string> target_file_list = get_all_file(local_addr);
            target_file_list.Sort();

            int last_iptr = -1;
            FileInfo fi = null;
            string rel_path = "";
            //对已有列表进行对比
            while (i_ptr < target_file_list.Count && j_ptr < ret.Count)
            {
                if (last_iptr != i_ptr)
                {
                    fi = new FileInfo(target_file_list[i_ptr]);
                    rel_path = relative_addr(local_addr, target_file_list[i_ptr]);
                    last_iptr = i_ptr;
                }
                
                switch (string.Compare(target_file_list[i_ptr], ret[j_ptr].path, true))
                {
                    case 1:
                        ret.RemoveAt(j_ptr);
                        
                        break;
                    case -1:
                        local_file_data data = new local_file_data(rel_path, calculate_md5(target_file_list[i_ptr]), fi.LastWriteTime, fi.Length);

                        ret.Insert(++j_ptr, data);
                        j_ptr++;
                        i_ptr++;
                        break;
                    case 0:
                        data = ret[j_ptr];
                        if (fi.LastWriteTime != data.modify_time && fi.Length != data.length)
                        {
                            data = new local_file_data(rel_path, calculate_md5(target_file_list[i_ptr]), fi.LastWriteTime, fi.Length);
                            ret[j_ptr] = data;
                        }
                        i_ptr++;
                        j_ptr++;
                        break;
                    default:
                        break;
                }
            }
            //删除多余的列表
            ret.RemoveRange(j_ptr, ret.Count - j_ptr);
            //添加到列表
            for (; i_ptr < target_file_list.Count; i_ptr++)
            {
                fi = new FileInfo(target_file_list[i_ptr]);
                rel_path = relative_addr(local_addr, target_file_list[i_ptr]);
                ret.Add(new local_file_data(rel_path, calculate_md5(target_file_list[i_ptr]), fi.LastWriteTime, fi.Length));
            }
            
            //数据排序
            ret.Sort();

            log_msg(LogType.DEBUG, "writing cache data (" + ret.Count + " file data)");
            //写入缓存数据
            fs = new FileStream(_phys_addr + "\\local\\" + local_addr_sha, FileMode.Create, FileAccess.Write);
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write(ret.Count);
                foreach (local_file_data item in ret)
                {
                    byte[] buf = System.Text.Encoding.Default.GetBytes(item.path);
                    bw.Write((short)buf.Length);
                    bw.Write(buf);
                    bw.Write(item.MD5);
                    bw.Write(item.modify_time.Ticks);
                    bw.Write(item.length);
                }
            }
            return ret;
        }
        //计算文件MD5,对应事件:Calculating_MD5(绝对路径,已读取文件大小,文件总大小)
        private byte[] calculate_md5(string file)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider md5_calc = new System.Security.Cryptography.MD5CryptoServiceProvider();
            //md5 calculate
            md5_calc.Initialize();

            log_msg(LogType.DEBUG, "calculating file md5: " + file);

            FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read);
            const int buf_size = 1048576; //1MB缓冲区
            byte[] buf = new byte[buf_size];

            long ofs = 0;
            if (fs.Length > 0)
            {
                while (ofs < fs.Length)
                {
                    long cur_sz = buf_size;
                    if (ofs + cur_sz > fs.Length)
                        cur_sz = fs.Length - ofs;

                    fs.Read(buf, 0, (int)cur_sz);
                    if (ofs + cur_sz < fs.Length)
                        md5_calc.TransformBlock(buf, 0, (int)cur_sz, buf, 0);
                    else
                        md5_calc.TransformFinalBlock(buf, 0, (int)cur_sz);

                    ofs += cur_sz;

                    if (Calculating_MD5 != null)
                        Calculating_MD5(file, ofs, fs.Length);
                }
            }
            else
                //面对0B文件不得不用杀手锏了
                md5_calc.TransformFinalBlock(new byte[0], 0, 0);

            fs.Close();

            byte[] ret = md5_calc.Hash;
            md5_calc.Clear();
            return ret;
        }
        /*
         * ******  **    **  ******  **   **  ******
         * **      **    **  **      ***  **    **
         * ******  **    **  ******  ** * **    **
         * **       **  **   **      **  ***    **
         * ******     **     ******  **   **    **
         */
        //汇报计算MD5的进度
        public delegate void Calculating_MD5_Handler(string abs_path, long cur_pos, long file_len);
        public event Calculating_MD5_Handler Calculating_MD5;

        #endregion
    }
}
