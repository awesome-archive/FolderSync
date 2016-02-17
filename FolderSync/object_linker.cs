//Project 2016 - Folder Sync v2
//Author: pandasxd (https://github.com/qhgz2013/FolderSync)
//
//object_linker.cs
//description: 用于保持文件的原位置

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FolderSync
{
    public partial class repo
    {
        #region Object_to_commit Ref Part
        /*
        private struct object_ref_data : IList<string>
        {
            public byte[] commit_SHA;
            public List<string> source_addr;
            public object_ref_data(byte[] b)
            {
                commit_SHA = b;
                source_addr = new List<string>();
            }
            public object_ref_data(string commit_SHA)
            {
                this.commit_SHA = util.hex(commit_SHA);
                source_addr = new List<string>();
            }
            public int IndexOf(string item)
            {
                return source_addr.IndexOf(item);
            }

            public void Insert(int index, string item)
            {
                source_addr.Insert(index, item);
            }

            public void RemoveAt(int index)
            {
                source_addr.RemoveAt(index);
            }

            public string this[int index]
            {
                get
                {
                    return source_addr[index];
                }
                set
                {
                    source_addr[index] = value;
                }
            }

            public void Add(string item)
            {
                source_addr.Add(item);
            }

            public void Clear()
            {
                source_addr.Clear();
            }

            public bool Contains(string item)
            {
                return source_addr.Contains(item);
            }

            public void CopyTo(string[] array, int arrayIndex)
            {
                source_addr.CopyTo(array, arrayIndex);
            }

            public int Count
            {
                get { return source_addr.Count; }
            }

            public bool IsReadOnly
            {
                get { return false; }
            }

            public bool Remove(string item)
            {
                return source_addr.Remove(item);
            }

            public IEnumerator<string> GetEnumerator()
            {
                return source_addr.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return source_addr.GetEnumerator();
            }
        }
        private struct object_ref_list : IComparable<string> , IList<object_ref_data>
        {
            public byte[] file_MD5;
            public List<object_ref_data> ref_list;
            public object_ref_list(byte[] b)
            {
                file_MD5 = b;
                ref_list = new List<object_ref_data>();
            }
            public object_ref_list(string file_MD5)
            {
                this.file_MD5 = util.hex(file_MD5);
                ref_list = new List<object_ref_data>();
            }
            public int CompareTo(string other)
            {
                return util.hex(file_MD5).ToLower().CompareTo(other);
            }

            public int IndexOf(object_ref_data item)
            {
                return ref_list.IndexOf(item);
            }

            public void Insert(int index, object_ref_data item)
            {
                ref_list.Insert(index, item);
            }

            public void RemoveAt(int index)
            {
                ref_list.RemoveAt(index);
            }

            public object_ref_data this[int index]
            {
                get
                {
                    return ref_list[index];
                }
                set
                {
                    ref_list[index] = value;
                }
            }

            public void Add(object_ref_data item)
            {
                ref_list.Add(item);
            }

            public void Clear()
            {
                ref_list.Clear();
            }

            public bool Contains(object_ref_data item)
            {
                return ref_list.Contains(item);
            }

            public void CopyTo(object_ref_data[] array, int arrayIndex)
            {
                ref_list.CopyTo(array, arrayIndex);
            }

            public int Count
            {
                get { return ref_list.Count; }
            }

            public bool IsReadOnly
            {
                get { return false; }
            }

            public bool Remove(object_ref_data item)
            {
                return ref_list.Remove(item);
            }

            public IEnumerator<object_ref_data> GetEnumerator()
            {
                return ref_list.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return ref_list.GetEnumerator();
            }
        }
        */
        private SortedList<byte[], SortedList<byte[], List<string>>>[] _ref_list;
        private class _tmp_Cmp : IComparer<byte[]>
        {
            public int Compare(byte[] x, byte[] y)
            {
                return string.Compare(util.hex(x), util.hex(y));
            }
        }
        private void init_ref_list()
        {
            log_msg(LogType.DEBUG, "init object referer list");

            _ref_list = new SortedList<byte[], SortedList<byte[], List<string>>>[256];
            _tmp_Cmp n = new _tmp_Cmp();
            long sum = 0;
            for (int i = 0; i < 256; i++)
            {
                _ref_list[i] = new SortedList<byte[], SortedList<byte[], List<string>>>(n);
                string target_file_addr = _phys_addr + "\\object\\" + i.ToString("X2").ToLower() + "\\ref";

                if (!File.Exists(target_file_addr) || (new FileInfo(target_file_addr).Length < 4))
                {
                    FileStream tmp_fs = new FileStream(target_file_addr, FileMode.Create, FileAccess.Write);
                    tmp_fs.Write(new byte[] { 0, 0, 0, 0 }, 0, 4);
                    tmp_fs.Close();
                }

                FileStream fs = new FileStream(target_file_addr, FileMode.Open, FileAccess.Read);
                using (BinaryReader br = new BinaryReader(fs))
                {
                    int len = br.ReadInt32();
                    for (int j = 0; j < len; j++) //object
                    {
                        byte[] file_md5 = br.ReadBytes(16);
                        SortedList<byte[], List<string>> ls = new SortedList<byte[], List<string>>(n);
                        int len2 = br.ReadInt32();
                        for (int k = 0; k < len2; k++) //commit
                        {
                            byte[] commit_SHA = br.ReadBytes(20);
                            List<string> file_list = new List<string>();
                            int len3 = br.ReadInt32();
                            for (int l = 0; l < len3; l++) //file
                            {
                                int len4 = br.ReadInt16();
                                file_list.Add(Encoding.Default.GetString(br.ReadBytes(len4)));
                                sum++;
                            }
                            ls.Add(commit_SHA, file_list);
                        }
                        _ref_list[i].Add(file_md5, ls);
                    }
                }
            }

            log_msg(LogType.DEBUG, "init object referer list completed, added " + sum + " link(s)");
        }
        private void update_ref_list()
        {
            log_msg(LogType.DEBUG, "updating object referer list");
            for (int i = 0; i < 256; i++)
            {
                string target_file_addr = _phys_addr + "\\object\\" + i.ToString("X2").ToLower() + "\\ref";

                FileStream fs = new FileStream(target_file_addr, FileMode.Create, FileAccess.Write);
                BinaryWriter bw = new BinaryWriter(fs);

                bw.Write(_ref_list[i].Count);
                foreach (KeyValuePair<byte[], SortedList<byte[], List<string>>> item in _ref_list[i])
                {
                    bw.Write(item.Key);
                    bw.Write(item.Value.Count);
                    foreach (KeyValuePair<byte[], List<string>> item2 in item.Value)
                    {
                        bw.Write(item2.Key);
                        bw.Write(item2.Value.Count);
                        foreach (string item3 in item2.Value)
                        {
                            byte[] buf = Encoding.Default.GetBytes(item3);
                            bw.Write((short)buf.Length);
                            bw.Write(buf);
                        }
                    }
                }

                bw.Close();
            }

            
;
        }
        private void create_link(byte[] MD5, string rel_addr, byte[] commit_SHA)
        {
            log_msg(LogType.DEBUG, "creating link: " + util.hex(MD5).ToLower() + " -> [" + util.hex(commit_SHA).ToLower() + "]" + rel_addr);

            _tmp_Cmp n = new _tmp_Cmp();
            
            if (MD5 == null || commit_SHA == null || string.IsNullOrEmpty(rel_addr))
                return;
            if (!_ref_list[MD5[0]].ContainsKey(MD5)) //md5不存在
                _ref_list[MD5[0]].Add(MD5, new SortedList<byte[], List<string>>(n));

            if (!_ref_list[MD5[0]][MD5].ContainsKey(commit_SHA)) //提交sha不存在
                _ref_list[MD5[0]][MD5].Add(commit_SHA, new List<string>());

            _ref_list[MD5[0]][MD5][commit_SHA].Add(rel_addr);
        }
        private void remove_link(byte[] MD5, string rel_addr, byte[] commit_SHA)
        {
            log_msg(LogType.DEBUG, "removing link: " + util.hex(MD5).ToLower() + " -> [" + util.hex(commit_SHA).ToLower() + "]" + rel_addr);

            if (MD5 == null || commit_SHA == null || string.IsNullOrEmpty(rel_addr))
                return;

            if (!_ref_list[MD5[0]].ContainsKey(MD5)) //md5不存在
                return;

            if (!_ref_list[MD5[0]][MD5].ContainsKey(commit_SHA)) //提交sha不存在
                return;

            _ref_list[MD5[0]][MD5][commit_SHA].Remove(rel_addr);

            if (_ref_list[MD5[0]][MD5][commit_SHA].Count == 0)
                _ref_list[MD5[0]][MD5].Remove(commit_SHA);

            if (_ref_list[MD5[0]][MD5].Count == 0)
                _ref_list[MD5[0]].Remove(MD5);


        }

        private bool can_delete_object(byte[] MD5)
        {
            if (MD5 == null)
                return false;
            return !_ref_list[MD5[0]].ContainsKey(MD5);
        }
        #endregion //Object_to_commit Ref Part
    }
}
