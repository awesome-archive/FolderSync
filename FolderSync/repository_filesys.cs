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
    public partial class repository : IDisposable
    {
        private bool _stat_failed;
        public repository()
        {
            //不进行初始化时，禁止调用所有要求有文件仓库的内部函数（全局）
            _stat_failed = true;
        }
        public repository(string root_location)
        {
            Open(root_location);
        }
        public void Open(string root_location)
        {
            _stat_failed = false;
            try
            {
                DirectoryInfo di = new DirectoryInfo(root_location);
                _repo_root_location = di.FullName;

                Init_repository();
            }
            catch (Exception)
            {
                _stat_failed = true;
                throw;
            }
            
        }

        private string _repo_root_location; //仓库根目录
        private string _repo_name;
        private string _repo_description;

        //private uint _folder_index; //文件夹索引

        private SQLiteConnection _repo_filesys_con; //仓库文件索引系统sql连接
        private SQLiteCommand _repo_filesys_cmd;
        private SQLiteConnection _repo_filever_con; //仓库文件索引版本系统sql连接
        private SQLiteCommand _repo_filever_cmd;
        private SQLiteConnection _repo_fileusg_con;
        private SQLiteCommand _repo_fileusg_cmd;

        //创建仓库（支持直接调用）
        public void Create_repository(string root_location)
        {
            //检查文件夹
            try
            {
                //文件夹不存在,创建
                if (!Directory.Exists(root_location))
                    Directory.CreateDirectory(root_location);
                else
                {
                    //文件夹存在,选择清空文件夹或是选择其他路径
                    bool result = false;
                    if (Ask_For_Delete_Directory != null)
                        Ask_For_Delete_Directory(root_location, ref result);
                    else
                        throw new InvalidOperationException("文件夹已存在");
                    //再次询问
                    if (result)
                        Ask_For_Delete_Directory(root_location, ref result);
                    else
                        throw new InvalidOperationException("操作已取消");
                    if (result)
                    {
                        //清空目标文件夹
                        Delete_Directory(root_location, false, true);
                        Delete_Directory(root_location, true, false);

                    }
                }
            }
            catch (Exception)
            {
                _stat_failed = true;
                throw new InvalidDataException("创建文件夹失败，是否为非法路径？或为权限不足");
            }

            //创建文件架构
            try
            {
                Directory.CreateDirectory(root_location + "/index");
                Directory.CreateDirectory(root_location + "/backup");
                Directory.CreateDirectory(root_location + "/data");
                for (int i = 0; i < 256; i++)
                    Directory.CreateDirectory(root_location + "/data/" + Others.Hex(new byte[] { (byte)i }));
                Directory.CreateDirectory(root_location + "/local_caches");
                FileStream fs = new FileStream(root_location + "/说明README.txt", FileMode.Create, FileAccess.Write);
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write("本目录由软件自动管理，请勿移动或删除任何一个文件夹，否则软件出错及数据丢失后果自负");
                }
                fs.Close();
                fs = new FileStream(root_location + "/global_settings", FileMode.Create, FileAccess.Write);
                var root_json = new JObject();
                root_json.Add("name", "repository");
                _repo_name = "repository";
                root_json.Add("description", "");
                using (var sw = new StreamWriter(fs))
                {
                    sw.Write(JsonConvert.SerializeObject(root_json));
                }
                fs.Close();
                File.Create(root_location + "/sign").Close();

                //创建sql文件
                Create_sql_table(root_location);
            }
            catch (Exception)
            {
                _stat_failed = true;
                throw;
            }
        }
        private void Create_sql_table(string root_location)
        {
            //文件系统
            SQLiteConnection.CreateFile(root_location + "/index/current.db");
            var con = new SQLiteConnection("Data Source=" + root_location + "/index/current.db; Version=3;");
            con.Open();
            var cmd = new SQLiteCommand(con);
            /*
            cmd.CommandText = "CREATE TABLE Folder(parent INT NOT NULL, child INT NOT NULL, type TINYINT NOT NULL)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "CREATE TABLE FolderData(UID INT PRIMARY KEY, ext_data TEXT NOT NULL)";
            cmd.ExecuteNonQuery();
            */
            cmd.CommandText = "CREATE TABLE File(folderUID INT, name TEXT NOT NULL, md5 BINARY(16), type TINYINT NOT NULL)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "CREATE TABLE FileData(folderUID INT, name TEXT NOT NULL, ext_data TEXT NOT NULL)";
            cmd.ExecuteNonQuery();
            /*
            cmd.CommandText = "CREATE TABLE Indexer(IndexName TEXT PRIMARY KEY, IndexValue INT NOT NULL)";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "INSERT INTO Indexer VALUES('FolderUIDIndexer', 0)";
            cmd.ExecuteNonQuery();
            */
            con.Close();

            //文件版本
            SQLiteConnection.CreateFile(root_location + "/index/commit_list.db");
            con = new SQLiteConnection("Data Source=" + root_location + "/index/commit_list.db; Version=3;");
            con.Open();
            cmd = new SQLiteCommand(con);

            cmd.CommandText = "CREATE TABLE commit_links(cFrom TEXT NOT NULL, cTo TEXT NOT NULL)";
            cmd.ExecuteNonQuery();

            con.Close();

            //文件引用
            SQLiteConnection.CreateFile(root_location + "/index/file_usage.db");
            con = new SQLiteConnection("Data Source=" + root_location + "/index/file_usage.db; Version=3");
            con.Open();
            cmd = new SQLiteCommand(con);

            cmd.CommandText = "CREATE TABLE file_refs(md5 BINARY(16) PRIMARY KEY, ref_times INT NOT NULL)";
            cmd.ExecuteNonQuery();

            con.Close();
        }
        private void Init_repository()
        {
            if (_stat_failed) throw new Exception("操作失败");

            try
            {
                if (!Directory.Exists(_repo_root_location))
                    throw new InvalidOperationException("仓库位置错误");
                if (!File.Exists(_repo_root_location + "/sign"))
                    throw new InvalidOperationException("仓库缺少标识文件");

                //loading name/description
                FileStream fs = new FileStream(_repo_root_location + "/global_settings", FileMode.Open, FileAccess.Read);
                JObject root_json = (JObject)JsonConvert.DeserializeObject(StreamUtils.ReadToEnd(fs));
                fs.Close();
                _repo_name = root_json.Value<string>("name");
                _repo_description = root_json.Value<string>("description");
                
                //creating sql connections
                _repo_filesys_con = new SQLiteConnection("Data Source=" + _repo_root_location + "/index/current.db; Version=3;");
                _repo_filesys_con.Open();
                _repo_filesys_cmd = new SQLiteCommand(_repo_filesys_con);
                _repo_filever_con = new SQLiteConnection("Data Source=" + _repo_root_location + "/index/commit_list.db; Version=3;");
                _repo_filever_con.Open();
                _repo_filever_cmd = new SQLiteCommand(_repo_filever_con);
                _repo_fileusg_con = new SQLiteConnection("Data Source=" + _repo_root_location + "/index/file_usage.db; Version=3;");
                _repo_fileusg_con.Open();
                _repo_filever_cmd = new SQLiteCommand(_repo_fileusg_con);
                /*
                //updating initial variables
                _repo_filesys_cmd.CommandText = "SELECT IndexValue FROM Indexer WHERE IndexName = 'FolderUIDIndexer'";
                SQLiteDataReader dr = _repo_filesys_cmd.ExecuteReader();
                dr.Read();
                _folder_index = (uint)dr.GetInt32(0);
                dr.Close();
                */
            }
            catch (Exception)
            {
                _stat_failed = true;
                throw;
            }
        }

        void IDisposable.Dispose()
        {
            this.Dispose();
        }
        private void Dispose()
        {
            if (_repo_filesys_con != null)
            {
                _repo_filesys_con.Close();
                _repo_filesys_con = null;
            }
            if (_repo_filever_con != null)
            {
                _repo_filever_con.Close();
                _repo_filever_con = null;
            }
            if (_repo_fileusg_con != null)
            {
                _repo_fileusg_con.Close();
                _repo_fileusg_con = null;
            }
        }
    }
}