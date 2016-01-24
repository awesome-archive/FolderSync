using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FolderSync
{
    public partial class repo
    {
        #region /local manager
        public struct local_file_data
	    {
            public string path;
            public string MD5;
            public string modify_time;
	    }
        public SortedList<string,local_file_data> dif(string local_addr,string commit="")
        {
            SortedList<string, local_file_data> ret = new SortedList<string, local_file_data>();

            if(commit.Length>0)
            {
                //todo: load commit
                
            }

            return ret;
        }
        #endregion
    }
}
