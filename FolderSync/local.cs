using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FolderSync
{
    public partial class repo
    {
        #region local

        public void Local_Add(string local_addr,string name)
        {
            if (local_list.ContainsKey(name))
                throw new NotSupportedException("已存在本地目录: " + name);
            local_list.Add(name, format_addr(local_addr));

            update_global();
        }
        public void Local_Rename(string old_name,string new_name)
        {
            if (!local_list.ContainsKey(old_name))
                throw new KeyNotFoundException("未找到本地目录：" + old_name);
            string path;
            local_list.TryGetValue(old_name, out path);
            local_list.Remove(old_name);
            local_list.Add(new_name, path);

            update_global();
        }
        public void Local_Modify(string new_addr,string name)
        {
            if (!local_list.ContainsKey(name))
                throw new KeyNotFoundException("未找到本地目录：" + name);
            local_list.Remove(name);
            local_list.Add(name, format_addr(new_addr));
            
            update_global();
        }
        public void Local_Delete(string name)
        {

            if (!local_list.ContainsKey(name))
                throw new KeyNotFoundException("未找到本地目录：" + name);
            local_list.Remove(name);

            update_global();
        }

        public string Local_Replace(string str)
        {
            bool rep;
            do
            {
                rep = false;
                foreach (KeyValuePair<string,string> item in local_list)
                {
                    if (str.IndexOf(item.Key) > 0)
                    {
                        str.Replace(item.Key, item.Value);
                        rep = true;
                    }

                }
            } while (rep);
            return str;
        }
        #endregion
    }
}
