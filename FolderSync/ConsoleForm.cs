//Project 2016 - Folder Sync v2
//Author: pandasxd (https://github.com/qhgz2013/FolderSync)
//
//ConsoleForm.cs
//description: 命令行UI
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FolderSync
{
    class ConsoleForm
    {

        #region command parser

        public void parseCommand(string cmd)
        {
            cmd = cmd.Split(new char[] { '\r', '\n' })[0];
            string[] arg_list = cmd.Split(' ');
            for (int i = 0; i < arg_list.Length; i++)
                arg_list[i] = arg_list[i].Trim('"');
            switch (arg_list[0])
            {
                case "commit":
                    if (string.IsNullOrEmpty(arg_list[1]))
                        return;
                    switch (arg_list[1])
                    {
                        //commit push <local addr> [-title title] [-root root_addr] [-description description] [-f]
                        case "push":
                            string title = "", desc = "", root = "", local_addr = "";
                            bool force = false;
                            if (string.IsNullOrEmpty(arg_list[2]))
                                return;
                            for (int i = 2; i < arg_list.Length; i++)
                            {
                                switch (arg_list[i])
                                {
                                    case "-title":
                                        title = arg_list[i + 1];
                                        i++;
                                        break;
                                    case "-description":
                                        desc = arg_list[i + 1];
                                        i++;
                                        break;
                                    case "-f":
                                        force = true;
                                        break;
                                    case "-root":
                                        root = arg_list[i + 1];
                                        i++;
                                        break;
                                    default:
                                        local_addr = arg_list[i];
                                        break;
                                }
                            }
                            //Commit_push(local_addr, title, desc, root, force);

                            break;

                        //commit delete <commit sha|-i index>
                        case "delete":
                            string commit_sha = "";
                            int index = -1;
                            for (int i = 2; i < arg_list.Length; i++)
                            {
                                switch (arg_list[i])
                                {
                                    case "-i":
                                        index = int.Parse(arg_list[i + 1]);
                                        i++;
                                        break;

                                    default:
                                        commit_sha = arg_list[i];
                                        break;
                                }
                            }
                            //if (!string.IsNullOrEmpty(commit_sha))
                            //Commit_delete(commit_sha);
                            //else if (index != -1)
                            //Commit_delete(index);

                            break;

                        //commit list
                        case "list":
                            /*
                            log_msg(LogType.DEBUG, "There are " + _commit_list.Count + " commits\r\n");
                            foreach (commit_list_data item in _commit_list)
                            {
                                StringBuilder sb = new StringBuilder();
                                sb.Append("[" + util.hex(item.commit_SHA).ToLower() + "] ");
                                sb.Append("title: " + item.title + "\r\n");
                                sb.Append("description: " + item.description + "\r\n");
                                log_msg(LogType.DEBUG, sb.ToString());
                            }
                            */
                            break;

                        default:
                            break;
                    }
                    break;

                case "local":
                    break;

                //list [-root root_addr] [commit sha|-i index]
                case "list":
                /*
                List<commit_full_data> ls;
                int index0 = _commit_list.Count - 1;
                string commit_sha0 = "", root0 = "";
                for (int i = 1; i < arg_list.Length; i++)
                {
                    switch (arg_list[i])
                    {
                        case "-i":
                            index0 = int.Parse(arg_list[i + 1]);
                            i++;
                            break;
                        case "-root":
                            root0 = arg_list[i + 1];
                            i++;
                            break;
                        default:
                            commit_sha0 = arg_list[i];
                            break;
                    }
                }
                if (!string.IsNullOrEmpty(commit_sha0))
                    ls = Get_commit_full_file_list(util.hex(commit_sha0), root0);
                else
                    ls = Get_commit_full_file_list(index0, root0);
                commit_sha0 = util.hex(Get_commit_data(index0).commit_SHA).ToLower();
                log_msg(LogType.DEBUG, "There are " + ls.Count + " files in commit " + commit_sha0);

                foreach (commit_full_data item in ls)
                    log_msg(LogType.DEBUG, "[" + util.hex(item.MD5).ToLower() + "] " + item.addr);

                break;
                */
                default:
                    break;
            }
        }


        #endregion
    }


}
