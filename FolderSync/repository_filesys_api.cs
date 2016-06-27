using System;
using System.Text;
using System.IO;
using System.Threading;
using VBUtil.Utils;
using System.Data.SQLite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace FolderSync
{
    public partial class repository
    {
        public struct ObjectMeta
        {
            public string Name;
            public string FullName;
            public string Extension;
            public bool isDir;
            public string MD5;
            public ulong Size;
        }
        public ObjectMeta Upload_File(string filename, string dst_path)
        {
            ObjectMeta ret = new ObjectMeta();
            if (_stat_failed) throw new Exception("操作失败");

            try
            {
                //备份文件索引
                File.Copy(_repo_root_location + "/index/current.db", _repo_root_location + "/index/current.db.bak", true);
                File.Copy(_repo_root_location + "/index/file_usage.db", _repo_root_location + "/index/file_usage.db.bak", true);

                ret.Name = _Get_File_Name(dst_path);
                ret.Extension = _Get_Extension(dst_path);
                ret.isDir = false;
                ret.FullName = dst_path;

                var fi = new FileInfo(filename);
                ret.Size = (ulong)fi.Length;

                //计算文件md5
                ret.MD5 = VBUtil.Utils.Others.Hex(_Get_File_MD5(filename));

                //不存在，复制
                if (!_File_Exists(ret.MD5))
                {
                    _Copy_File(fi.FullName, _repo_root_location + "/data/" + ret.MD5.Substring(0, 2) + "/" + ret.MD5);
                }

                //链接到文件目录系统
                #region Add Link

                #endregion
            }
            catch(Exception)
            {
                _stat_failed = true;
                Dispose();
                throw new Exception("上传文件错误");
            }

            return ret;
        }
        /// <summary>
        /// 搜索文件是否已包含在系统内
        /// </summary>
        /// <param name="md5"></param>
        /// <returns></returns>
        private bool _File_Exists(string md5)
        {
            _repo_fileusg_cmd.CommandText = "SELECT ref_times FROM file_refs WHERE md5 = 0x" + md5;
            SQLiteDataReader dr = _repo_fileusg_cmd.ExecuteReader();
            bool suc = dr.Read();
            dr.Close();
            return suc;
        }
        
        public bool Dir_Exists(string path)
        {
            if (_stat_failed) throw new Exception("操作失败");

            try
            {
                string[] dir = path.Split('/');
                if (dir.Length == 1) return true;
                else if (dir.Length == 0) throw new ArgumentException("路径不合法");

                uint cur_fUID = 0;
                for (int i = 1; i < dir.Length; i++)
                {
                    _repo_filesys_cmd.CommandText = "SELECT md5 FROM File WHERE (folderUID=" + cur_fUID + " AND name='" + dir[i] + "')";
                    SQLiteDataReader dr = _repo_filesys_cmd.ExecuteReader();
                    if (!dr.Read())
                    {
                        dr.Close();
                        return false;
                    }
                    byte[] buf = new byte[16];
                    dr.GetBytes(0, 0, buf, 0, 16);
                    cur_fUID = VBUtil.Utils.ByteUtils.ByteToUInt(buf);
                    dr.Close();
                }
                return true;
            }
            catch (Exception)
            {
                _stat_failed = true;
                Dispose();
                throw;
            }
        }
        public void Dir_Create(string path)
        {
            if (_stat_failed) throw new Exception("操作失败");

            try
            {
                string[] dir = path.Split('/');
                if (dir.Length == 1) return;
                else if (dir.Length == 0) throw new ArgumentException("路径不合法");

                uint cur_fUID = 0;
                for (int i = 1; i < dir.Length; i++)
                {
                    _repo_filesys_cmd.CommandText = "SELECT md5 FROM File WHERE (folderUID=" + cur_fUID + " AND name='" + dir[i] + "')";
                    SQLiteDataReader dr = _repo_filesys_cmd.ExecuteReader();
                    if (!dr.Read())
                    { //找不到路径
                        dr.Close();
                        uint next_fUID = _Generate_Random_ID();
                        _repo_filesys_cmd.CommandText = "INSERT INTO File VALUES(" + cur_fUID + ", '" + dir[i] + "', 0x" + VBUtil.Utils.Others.Hex(VBUtil.Utils.ByteUtils.UIntToByte(next_fUID)) + ", 128)";
                        _repo_filesys_cmd.ExecuteNonQuery();
                        cur_fUID = next_fUID;
                    }
                    else
                    {
                        byte[] buf = new byte[16];
                        dr.GetBytes(0, 0, buf, 0, 16);
                        cur_fUID = VBUtil.Utils.ByteUtils.ByteToUInt(buf);
                        dr.Close();

                    }
                }


            }
            catch (Exception)
            {
                _stat_failed = true;
                Dispose();
                throw;
            }
        }
        private uint _Generate_Random_ID()
        {
            byte[] buf = new byte[4];
            VBUtil.Utils.Others.rand.NextBytes(buf);
            return VBUtil.Utils.ByteUtils.ByteToUInt(buf);
        }
    }
}