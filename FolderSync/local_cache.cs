using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FolderSync
{
    public partial class repo
    {
        #region /local manager
        public struct local_file_data
	    {
            public string path;
            public byte[] MD5;
            public DateTime modify_time;
	    }
        private string relative_addr(string root,string file)
        {
            return file.Replace(root, "");
        }
        private string absolute_addr(string root,string file)
        {
            if (file[0] == '\\')
                return root + file;
            else
                return root + "\\" + file;
        }
        public List<local_file_data> create_local_cache(string local_addr)
        {
            List<local_file_data> ret = new List<local_file_data>();
            local_addr = format_addr(Local_Replace(format_addr(local_addr)));
            System.Security.Cryptography.SHA1CryptoServiceProvider sha = new System.Security.Cryptography.SHA1CryptoServiceProvider();

            string local_addr_sha = util.hex(sha.ComputeHash(System.Text.Encoding.Default.GetBytes(local_addr))).ToLower();

            if ((!File.Exists(_phys_addr + "\\local\\" + local_addr_sha)) || (new FileInfo(_phys_addr + "\\local\\" + local_addr_sha)).Length < 4)
            {
                using (FileStream _fs = new FileStream(_phys_addr + "\\local\\" + local_addr_sha, FileMode.Create, FileAccess.ReadWrite))
                {
                    byte[] buf = new byte[4];
                    _fs.Write(buf, 0, 4);
                }
            }

            //load cache file
            FileStream fs = new FileStream(_phys_addr + "\\local\\" + local_addr_sha, FileMode.Open, FileAccess.ReadWrite);
            using (BinaryReader br = new BinaryReader(fs))
            {
                int len = br.ReadInt32();
                local_file_data data = new local_file_data();
                data.MD5 = new byte[16];
                for (int i = 0; i < len; i++)
                {
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

            if (!Directory.Exists(local_addr))
                return ret;

            int i_ptr = 0;
            //compare file
            foreach (string file_name in Directory.GetFiles(local_addr))
            {
                string rel_path = relative_addr(local_addr, file_name);
                FileInfo fi = new FileInfo(file_name);
                local_file_data data = ret.ElementAtOrDefault(i_ptr);
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
                    if (string.Compare(data.path, rel_path) < 0)
                    {
                        while ((i_ptr < ret.Count) && (string.Compare(ret.ElementAtOrDefault(i_ptr).path, rel_path) < 0))
                        {
                            if (Log != null)
                                Log(LogType.DEBUG, "- " + ret.ElementAtOrDefault(i_ptr).path);

                            ret.RemoveAt(i_ptr);

                        }
                        data = ret.ElementAtOrDefault(i_ptr);
                    }
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

            //write cache file
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

        private byte[] calculate_md5(string file)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider md5_calc = new System.Security.Cryptography.MD5CryptoServiceProvider();
            //md5 calculate
            md5_calc.Initialize();

            FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read);
            const int buf_size = 1048576; //buf size for 1MB
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
                md5_calc.TransformFinalBlock(new byte[0], 0, 0);

            fs.Close();

            byte[] ret = md5_calc.Hash;
            md5_calc.Clear();
            return ret;
        }
        //event
        public delegate void Calculating_MD5_Handler(string abs_path, long cur_pos, long file_len);
        public event Calculating_MD5_Handler Calculating_MD5;

        #endregion
    }
}
