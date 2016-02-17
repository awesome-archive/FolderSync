//Project 2016 - Folder Sync v2
//Author: pandasxd (https://github.com/qhgz2013/FolderSync)
//
//repo.cs
//description: 仓库类的主要属性的定义和修改，仓库的创建和删除

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FolderSync
{
    public partial class repo : IDisposable
    {
        /// <summary>
        /// 规格化绝对路径（将"/"或"\"统一替换成"\"，并去掉最后一个"\"
        /// </summary>
        /// <param name="str">路径</param>
        /// <returns></returns>
        public static string format_addr(string str)
        {
            return str.Replace("/", "\\").TrimEnd('\\');
        }
        
        /// <summary>
        /// 默认最大提交数，若提交数超过此值，则之前的提交被划入不可修改的区域
        /// </summary>
        public const int DEFAULT_MAX_COMMIT_AVAILABLE = 100;

        
        #region repo create <repo name> <phys addr> [-desc <description>] [-f]

        private static void init_repo(string path, string desc = "")
        {
            #region GLOBAL

            IniParser.FileIniDataParser global = new IniParser.FileIniDataParser();
            IniParser.Model.IniData global_data = new IniParser.Model.IniData();

            util.addIniData(ref global_data, "global.description", "\"" + desc + "\"");
            

            util.addIniData(ref global_data, "property.max_commit_available", DEFAULT_MAX_COMMIT_AVAILABLE.ToString());

            global_data.Sections.AddSection("local");
            global.WriteFile(path + "\\GLOBAL", global_data, Encoding.Default);
            #endregion

            #region /commit
            Directory.CreateDirectory(path + "\\commit");
            //File.Create(path + "\\commit\\commitList");
            #endregion

            #region /object
            Directory.CreateDirectory(path + "\\object");
            for (int i = 0; i < 256; i++)
                Directory.CreateDirectory(path + "\\object\\" + i.ToString("X2").ToLower());
            #endregion

            #region /local
            Directory.CreateDirectory(path + "\\local");
            #endregion
        }
        /// <summary>
        /// 创建仓库
        /// </summary>
        /// <param name="path">仓库路径</param>
        /// <param name="name">仓库名称</param>
        /// <param name="desc">仓库描述</param>
        /// <param name="force">强制创建(该文件夹下的所有文件会被删除)</param>
        /// <returns>仓库或null(没有-f且不覆盖原路径下)</returns>
        public static repo Create(string path, string desc = "", bool force = false)
        {
            path = format_addr(path);
            if (Directory.Exists(path))
            {
                if (force)
                {
                    Directory.Delete(path, true);
                    System.Threading.Thread.Sleep(10);
                    while (!Directory.Exists(path)) //这里不知道为毛会间歇性羊癫疯(创建失败)
                        Directory.CreateDirectory(path);
                }
                else
                    if (!(System.Windows.Forms.MessageBox.Show("目录 " + path + " 已存在，是否覆盖原目录\n(该目录下的数据会丢失)", "错误", System.Windows.Forms.MessageBoxButtons.YesNoCancel) == System.Windows.Forms.DialogResult.Yes))
                        return null;
                    else
                    {
                        Directory.Delete(path, true);
                        System.Threading.Thread.Sleep(10);
                        while (!Directory.Exists(path))
                            Directory.CreateDirectory(path);
                    }

                init_repo(path, desc);
                return new repo(path);
            }
            Directory.CreateDirectory(path);
            init_repo(path, desc);
            return new repo(path);
        }
        #endregion //repo create <repo name> <phys addr> [-desc <description>] [-f]

        #region global_vars
        private string _phys_addr;
        private string _description;
        public string Description
        {
            get { return _description; }
            set { _description = value; update_global(); }
        }

        private Dictionary<string, string> local_list;
        private int _max_commit_available;
        public int Max_commit_available
        {
            get { return _max_commit_available; }
            set { _max_commit_available = value; update_global(); }
        }
        #endregion //global_vars

        #region global
        /// <summary>
        /// 保存/GLOBAL设置
        /// </summary>
        private void update_global()
        {
            log_msg(LogType.DEBUG, "updating global");
            IniParser.FileIniDataParser file = new IniParser.FileIniDataParser();
            IniParser.Model.IniData data = new IniParser.Model.IniData();
            util.addIniData(ref data, "global.description", "\"" + _description + "\"");
            util.addIniData(ref data, "property.max_commit_available", _max_commit_available.ToString());

            IniParser.Model.SectionData local = new IniParser.Model.SectionData("local");
            foreach (KeyValuePair<string, string> item in local_list)
                local.Keys.AddKey(item.Key, "\"" + item.Value + "\"");

            data.Sections.Add(local);

            file.WriteFile(_phys_addr + "\\GLOBAL", data, Encoding.Default);
        }
        /// <summary>
        /// 读取/GLOBAL设置
        /// </summary>
        private void init_global()
        {
            local_list = new Dictionary<string, string>();
            log_msg(LogType.DEBUG, "init global");

            IniParser.FileIniDataParser file = new IniParser.FileIniDataParser();
            IniParser.Model.IniData data = file.ReadFile(_phys_addr + "\\GLOBAL", Encoding.Default);
            data.TryGetKey("global.description", out _description);
            _description = _description.Trim('\"');

            string temp;
            data.TryGetKey("property.max_commit_available", out temp);
            _max_commit_available = int.Parse(temp);

            IniParser.Model.SectionData local = data.Sections.GetSectionData("local");
            foreach (IniParser.Model.KeyData item in local.Keys)
                local_list.Add(item.KeyName, item.Value.Trim('\"'));

        }

        #endregion //global

        /*
         * ******  **    **  ******  **   **  ******
         * **      **    **  **      ***  **    **
         * ******  **    **  ******  ** * **    **
         * **       **  **   **      **  ***    **
         * ******     **     ******  **   **    **
         */
        #region log
        public enum LogType
	    {
            DEBUG,WARN,ERROR
	    }
        public delegate void LoggerHandler(LogType type, string msg);
        public event LoggerHandler Log;
        private void log_msg(LogType type, string msg)
        {
            if (Log != null) Log(type, msg);
        }
        #endregion //log
        public repo(string path)
        {
            _phys_addr = format_addr(path);

            log_msg(LogType.DEBUG, "Begin init repo in " + _phys_addr);

            if(!(Directory.Exists(_phys_addr) && File.Exists(_phys_addr + "\\GLOBAL")))
            {
                Create(path, "", true);
                //throw new InvalidDataException("错误的仓库目录");
            }

            init_global();

            init_commit_list();

            init_ref_list();

            init_async();
        }
        public void Dispose()
        {
            update_global();
            update_commit_list();
            update_ref_list();
            update_async();
            _async_thread_stop_flag = true;
            //_async_thread.Join();
        }
    }
}
