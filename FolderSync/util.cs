using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FolderSync
{
    class util
    {
        public static string right(string str,int len)
        {
            if (str.Length <= len)
                return str;
            return str.Substring(str.Length - len, len);
        }

        public static void addIniData(ref IniParser.Model.IniData data, string key, string val)
        {
            int dot_loc = key.IndexOf(".");
            string sect_name = key.Substring(0, dot_loc);
            string data_name = key.Substring(dot_loc + 1);
            if (!data.Sections.ContainsSection(sect_name))
                data.Sections.AddSection(sect_name);
            IniParser.Model.SectionData dat = data.Sections.GetSectionData(sect_name);
            if (!dat.Keys.ContainsKey(data_name))
                dat.Keys.AddKey(data_name, val);
            else
            {
                IniParser.Model.KeyData key_data = new IniParser.Model.KeyData(data_name);
                key_data.Value = val;
                dat.Keys.SetKeyData(key_data);
            }
            data.Sections.SetSectionData(sect_name, dat);
        }

        public static string hex(byte[] arr)
        {
            StringBuilder sb = new StringBuilder();
            if (arr == null)
                return sb.ToString();
            for (int i = 0; i < arr.Length; i++)
                sb.Append(arr.ElementAt(i).ToString("X2"));

            return sb.ToString();
        }
        public static byte[] hex(string str)
        {
            if (str == null)
                return new byte[0];
            if ((str.Length % 2) != 0)
                str += " ";
            byte[] ret = new byte[str.Length / 2];
            for (int i = 0; i < ret.Length; i++)
                ret[i] = Convert.ToByte(str.Substring(i * 2, 2), 16);
            return ret;
        }
    }
}
