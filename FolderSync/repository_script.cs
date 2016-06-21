using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace FolderSync
{
    public partial class repository
    {
        /// <summary>
        /// 执行脚本文件
        /// </summary>
        /// <param name="script_path">脚本文件路径</param>
        public void Execute_Script(string script_path)
        {
            var fs = new FileStream(script_path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var str = VBUtil.Utils.StreamUtils.ReadToEnd(fs);
            fs.Close();

            Execute_Script_Text(str);
        }
        /// <summary>
        /// 执行脚本内容
        /// </summary>
        /// <param name="script_str">脚本的字符串内容</param>
        public void Execute_Script_Text(string script_str)
        {
            var ss = new ScriptStatus();
            ss.version = 1;
            ss.env_arg = new Dictionary<string, string>();

            foreach (string item in script_str.Split('\n'))
            {
                Parse_Single_Command(item.Replace("\r", "").Trim(), ref ss);
            }
        }
        //脚本状态
        private struct ScriptStatus
        {
            public int version; //脚本版本，默认1，为后面的兼容做准备
            public Dictionary<string, string> env_arg; //环境变量，暂时还没开发，以后也会用到
        }
        /// <summary>
        /// 将一个指令拆分成若干个参数，支持乱用空格，专业逼死强迫症的指令（不过在引号里面作死我也没办法了，这可要自己看着办了）
        /// </summary>
        /// <param name="cmd">指令内容</param>
        /// <returns>分隔后的指令参数</returns>
        private string[] Split_command(string cmd)
        {
            var ret = new List<string>();
            string arg = "";
            bool ws = false;
            bool quot = false;
            for (int i = 0; i < cmd.Length; i++)
            {
                if (cmd[i] == '"')
                {
                    quot = !quot;
                    continue;
                }
                if (cmd[i] == ' ' && !quot)
                {
                    if (!ws)
                    {
                        ret.Add(arg);
                        arg = "";
                    }
                    ws = true;
                    continue;
                }
                arg += cmd[i];
                ws = false;
            }

            if (quot) throw new InvalidDataException("指令缺少匹配的引号");
            if (arg.Length > 0) ret.Add(arg);
            return ret.ToArray();
        }
        /// <summary>
        /// 解析并执行单个指令
        /// </summary>
        /// <param name="cmd">指令的字符串</param>
        /// <param name="stat">脚本参数</param>
        private void Parse_Single_Command(string cmd, ref ScriptStatus stat)
        {
            string[] cmd_arg = Split_command(cmd);
            if (cmd_arg.Length == 0) return;

            //忽略调用函数的大小写
            switch (cmd_arg[0].ToLower())
            {
                //指令: version
                #region version command
                case "version":
                    //参数不为两个，报错
                    if (cmd_arg.Length != 2)
                        throw new ArgumentNullException("参数错误");
                    int test_version;
                    //版本号不为数字，同样报错
                    if (!int.TryParse(cmd_arg[1], out test_version))
                        throw new ArgumentException("版本号为非数字");
                    //最后一个作死的，还是报错
                    if (test_version <= 0 || test_version > 1)
                        throw new ArgumentOutOfRangeException("版本号超出范围（只为1）");
                    stat.version = test_version;
                    break;
                #endregion

                //指令: sync
                #region sync command
                case "sync":
                    //会不会switch - case语句有点多....
                    if (cmd_arg.Length < 3)
                        throw new ArgumentNullException("参数错误");
                    //指令的附加参数
                    var inc_ext = new List<string>();
                    var esc_ext = new List<string>();
                    bool hd_included = false;
                    int stachk_flag = FLAG_STACHK_DIRECT_SYNC_DEFAULT;

                    for (int i = 3; i < cmd_arg.Length; i++)
                    {
                        switch (cmd_arg[i])
                        {
                            case "--ext":
                                i++;
                                if (i >= cmd_arg.Length) throw new ArgumentNullException("缺少指令参数: " + cmd_arg[i - 1]);
                                foreach (string item in cmd_arg[i].Split(';'))
                                    esc_ext.Add(item.Trim());
                                break;
                            case "-+ext":
                                i++;
                                if (i >= cmd_arg.Length) throw new ArgumentNullException("缺少指令参数: " + cmd_arg[i - 1]);
                                foreach (string item in cmd_arg[i].Split(';'))
                                {
                                    if (item == "*") //全包括
                                    {
                                        inc_ext.Clear();
                                        break;
                                    }
                                    inc_ext.Add(item.Trim());
                                }
                                break;
                            case "-H": case "-hidden":
                                hd_included = true;
                                break;
                            case "-stachk":
                                i++;
                                if (i >= cmd_arg.Length) throw new ArgumentNullException("缺少指令参数: " + cmd_arg[i - 1]);
                                stachk_flag = 0;
                                foreach (string item in cmd_arg[i].Split(';'))
                                    switch (item)
	                                {
                                        case "stachk_md5":
                                            stachk_flag |= FLAG_STACHK_MD5;
                                            break;
                                        case "stachk_length":
                                            stachk_flag |= FLAG_STACHK_LENGTH;
                                            break;
                                        case "stachk_last_modified":
                                            stachk_flag |= FLAG_STACHK_LAST_MODIFIED_TIME;
                                            break;
                                        case "stachk_created":
                                            stachk_flag |= FLAG_STACHK_CREATED_TIME;
                                            break;
                                        case  "stachk_last_accessed":
                                            stachk_flag |= FLAG_STACHK_LAST_ACCESSED_TIME;
                                            break;
                                        default:
                                            throw new ArgumentException("未知指令参数: " + item);
	                                }

                                break;
                            default:
                                throw new ArgumentException("未知指令参数: " + cmd_arg[i]);
                        }
                        
                    }

                    Direct_Sync_File(cmd_arg[1], cmd_arg[2], stachk_flag, hd_included, inc_ext, esc_ext);
                    break;
                #endregion

                //指令：script
                #region script command
                case "script":
                    if (cmd_arg.Length != 2) throw new ArgumentException("参数错误");
                    if (string.IsNullOrEmpty(cmd_arg[1])) throw new ArgumentNullException("脚本文件为空");
                    Execute_Script(cmd_arg[1]);
                    break;
                #endregion

                default:
                    throw new ArgumentException("未知指令: " + cmd_arg[0]);
            }

        }
    }
}