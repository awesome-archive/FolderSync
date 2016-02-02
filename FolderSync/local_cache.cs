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
            public int CompareTo(local_file_data data)
            {
                return path.CompareTo(data.path);
            }
            public local_file_data(string path, byte[] MD5, DateTime modify_time)
            {
                this.path = path;
                this.MD5 = MD5;
                this.modify_time = modify_time;
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
            //文件结构很简单(所有数字类型的高位在上!),写入也同理
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
                    ret.Add(data);
                }


            }

            int i_ptr = 0;
            //下标比较,前提：要排序
            List<string> target_file_list = get_all_file(local_addr);
            target_file_list.Sort();
            foreach (string file_name in target_file_list)
            {
                //地址相对化
                string rel_path = relative_addr(local_addr, file_name);
                FileInfo fi = new FileInfo(file_name);
                //读取缓存列表中对应的文件
                local_file_data data = ret.ElementAtOrDefault(i_ptr);
                //缓存列表中的文件数据为空,理应是缓存列表文件的数目少于目标文件夹的文件数目,添加到列表中
                if (data.path == null)
                {
                    data.path = rel_path;
                    data.MD5 = calculate_md5(file_name);
                    data.modify_time = fi.LastWriteTime;
                    ret.Insert(i_ptr++, data);
                    data = ret.ElementAtOrDefault(i_ptr);

                    if (Log != null)
                        Log(LogType.DEBUG, "+ " + rel_path);
                }
                else
                {
                    //删除缓存列表中多出的文件
                    while ((i_ptr < ret.Count) && (string.Compare(ret.ElementAtOrDefault(i_ptr).path, rel_path) < 0))
                    {
                        if (Log != null)
                            Log(LogType.DEBUG, "- " + ret.ElementAtOrDefault(i_ptr).path);

                        ret.RemoveAt(i_ptr);
                    }
                    data = ret.ElementAtOrDefault(i_ptr);
                    //缓存列表中缺少此文件,添加到缓存列表中
                    if (string.Compare(data.path, rel_path) > 0)
                    {
                        data.path = rel_path;
                        data.MD5 = calculate_md5(file_name);
                        data.modify_time = fi.LastWriteTime;
                        ret.Insert(i_ptr++, data);
                        data = ret.ElementAtOrDefault(i_ptr);

                        if (Log != null)
                            Log(LogType.DEBUG, "+ " + rel_path);
                    }
                }
                //已经出现在缓存列表中的,比较最后一次修改时间,若有不同,则更新缓存信息
                if (string.Compare(data.path, rel_path) == 0)
                {
                    if (data.modify_time != fi.LastWriteTime)
                    {
                        data.MD5 = calculate_md5(file_name);
                        data.modify_time = fi.LastWriteTime;
                        ret.RemoveAt(i_ptr);
                        ret.Insert(i_ptr, data);

                        if (Log != null)
                            Log(LogType.DEBUG, "~ " + rel_path);
                    }
                    i_ptr++;
                }
            }

            //数据排序
            
            ret.Sort();
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
        //汇报计算MD5的进度,每读取1MB调用一次
        public delegate void Calculating_MD5_Handler(string abs_path, long cur_pos, long file_len);
        public event Calculating_MD5_Handler Calculating_MD5;

        #endregion
    }
}
