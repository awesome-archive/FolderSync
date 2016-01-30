using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FolderSync
{
    public partial class repo
    {
        #region commit
        public void Commit_push(string local_addr, string msg = "", string root = "/", bool force = false, bool quiet = false)
        {

        }

        /*
        public SortedList<string,commit_list_data> Get_commit_file(string commit_SHA)
        {
            SortedList<string, commit_list_data> ret = new SortedList<string, commit_list_data>();
            if (commit_SHA.Length <= 0) return ret;

            if (File.Exists(_phys_addr + "\\commit\\" + commit_SHA + "_full"))
            {
                //load full file

            }
            else
            {
                int last_index = Get_commit_index(commit_SHA) - 1;
                if (last_index > 0)
                {
                    ret = Get_commit_file(last_index);
                }
                //load changes

                //save full file

            }

            return ret;
        }
        */
        #region /commit/commitList
        public struct commit_list_data
        {
            public byte[] commit_SHA;
            public string description;
            public int stat;
        }
        private List<commit_list_data> _commit_list;
        public string Get_commit_SHA(int index)
        {
            if (_commit_list.Count <= index)
                throw new IndexOutOfRangeException("获取提交SHA值时下标越界， index=" + index.ToString());
            return util.hex(_commit_list.ElementAt(index).commit_SHA);
        }
        public int Get_commit_index(string commit_SHA)
        {
            byte[] tmp = util.hex(commit_SHA);
            return _commit_list.FindIndex(commit => commit.commit_SHA == tmp);
        }

        private void init_commit_list()
        {
            _commit_list = new List<commit_list_data>();
            if (!File.Exists(_phys_addr + "\\commit\\commitList"))
            {
                byte[] buf = new byte[4];
                FileStream _fs = new FileStream(_phys_addr + "\\commit\\commitList", FileMode.CreateNew, FileAccess.Write);
                _fs.Write(buf, 0, 4);
                _fs.Close();
            }
            FileStream fs = new FileStream(_phys_addr + "\\commit\\commitList", FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);

            uint listSize = br.ReadUInt32();
            commit_list_data data;
            data.commit_SHA = new byte[20];
            for (int i = 0; i < listSize; i++)
            {
                br.Read(data.commit_SHA, 0, 20);
                ushort str_len = br.ReadUInt16();
                if (str_len > 0)
                {
                    byte[] tmp = new byte[str_len];
                    br.Read(tmp, 0, str_len);
                    data.description = Encoding.Default.GetString(tmp);
                }
                else
                    data.description = string.Empty;
                data.stat = br.ReadInt32();
                _commit_list.Add(data);
            }

            br.Close();
        }
        private void update_commit_list()
        {
            FileStream fs = new FileStream(_phys_addr + "\\commit\\commitList", FileMode.Create, FileAccess.Write);
            BinaryWriter bw = new BinaryWriter(fs);

            bw.Write(_commit_list.Count);

            for (int i = 0; i < _commit_list.Count; i++)
            {
                bw.Write(_commit_list.ElementAt(i).commit_SHA);
                byte[] tmp = Encoding.Default.GetBytes(_commit_list.ElementAt(i).description);
                bw.Write((ushort)tmp.Length);
                bw.Write(tmp);
                bw.Write(_commit_list.ElementAt(i).stat);
            }

            bw.Close();
        }
        #endregion

        #region /commit/[SHA delta commit]
        public struct commit_delta_data
        {
            byte operation;
            string addr;
            byte[] MD5;
        }
        public List<commit_delta_data> Get_commit_file(string commit_SHA)
        {
            List<commit_delta_data> ret = new List<commit_delta_data>();

            return ret;
        }
        #endregion

        #endregion
    }
}
