using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using VBUtil.Utils;

namespace FolderSync
{
    public partial class repository
    {
        //默认数据流大小: 1MB
        public const int STREAM_BUFFER_SIZE = 0x100000;

        //文件一致性检验标识符

        //文件md5检验
        public const int FLAG_STACHK_MD5 = 1;
        //文件长度检验
        public const int FLAG_STACHK_LENGTH = 2;
        //文件最后修改时间检验
        public const int FLAG_STACHK_LAST_MODIFIED_TIME = 4;
        //文件创建时间检验
        public const int FLAG_STACHK_CREATED_TIME = 8;
        //文件最后访问时间检验
        public const int FLAG_STACHK_LAST_ACCESSED_TIME = 16;

        //注意:文件的时间属性会因文件系统不不同而产生差异!

        //默认的直接同步文件检验标识(长度+最后修改时间)
        public const int FLAG_STACHK_DIRECT_SYNC_DEFAULT = FLAG_STACHK_LAST_MODIFIED_TIME | FLAG_STACHK_LENGTH;
        
        /// <summary>
        /// 同步文件夹，使两文件夹的数据保持一致（支持直接调用）
        /// </summary>
        /// <param name="origin">源文件夹的绝对路径</param>
        /// <param name="destination">目标文件夹的绝对路径</param>
        /// <param name="STACHK_flag">文件一致性检验标识（见"FLAG_STACHK_xxx"），如有多个标识，请使用或运算（"|"）</param>
        public void Direct_Sync_File(string origin, string destination, int STACHK_flag = FLAG_STACHK_DIRECT_SYNC_DEFAULT, bool include_hidden_dir_and_file = false, List<string> include_ext = null, List<string> escape_ext = null)
        {
            //todo: 加上文件拓展名检验排除

            //验证文件夹合理与否 & 创建目标文件夹
            DirectoryInfo odi, ddi;
            odi = new DirectoryInfo(origin);
            if (odi.Exists == false)
            {
                //源文件夹不存在，抛出异常，退出函数
                throw new DirectoryNotFoundException("同步源文件夹 " + origin + " 不存在");
            }
            ddi = new DirectoryInfo(destination);
            if (ddi.Exists == false)
            {
                //目标文件夹不存在，创建
                ddi.Create();
            }

            //二期新增代码 - 初始化拓展名表
            if (include_ext == null) include_ext = new List<string>();
            if (escape_ext == null) escape_ext = new List<string>();
            bool has_include_ext = include_ext.Count > 0 ? true : false;
            bool has_escape_ext = escape_ext.Count > 0 ? true : false;
            //拓展名重复检测
            if(has_include_ext && has_escape_ext)
            {
                IEnumerable<string> repeat_ext = include_ext.Intersect(escape_ext);
                foreach (string item in repeat_ext)
                    throw new InvalidDataException("在包括和排除的拓展名中重复出现：" + item);
            }

            //获取文件目录
            odi = new DirectoryInfo(origin);
            ddi = new DirectoryInfo(destination);

            //源文件夹和目标文件夹里的文件分为3中状态
            //1 源文件夹有,目标文件夹没有,则可以无视文件检验过程(直接复制到新的文件夹中)
            //2 目标文件夹有,源文件夹没有,也可以无视文件检验过程(直接从目标文件夹里删除)
            //3 两个都有,则要进行文件验证
            List<string> ls_str_ofi = new List<string>(), ls_str_dfi = new List<string>();
            foreach (FileInfo item in odi.GetFiles())
            {
                //按后缀名分类
                if (has_include_ext)
                    if (!include_ext.Contains(item.Extension)) continue;
                if (has_escape_ext)
                    if (escape_ext.Contains(item.Extension)) continue;
                //二期代码 - 隐藏文件会因没有开启hidden而跳过
                if (!include_hidden_dir_and_file && (item.Attributes & FileAttributes.Hidden) != 0) continue;

                ls_str_ofi.Add(item.Name);
            }

            foreach (FileInfo item in ddi.GetFiles())
                ls_str_dfi.Add(item.Name);

            //集合运算: 源文件名 - 目标文件名  -> 要复制的文件名
            IEnumerable<string> copy_file = ls_str_ofi.Except(ls_str_dfi);
            //集合运算: 目标文件名 - 源文件名 -> 要删除的文件名
            IEnumerable<string> delete_file = ls_str_dfi.Except(ls_str_ofi);
            //集合运算: 源文件名 ∩ 目标文件名 -> 要验证复制的文件名
            IEnumerable<string> copy_to_be_validated = ls_str_ofi.Intersect(ls_str_dfi);

            //todo: 引发一下文件IO异常，验证代码逻辑 → 这么作死谁拦你啊(╯‵□′)╯︵┻━┻
            #region File_Operation
            //复制文件开始
            foreach (string item in copy_file)
            {
                bool suc = false;
                do
                {
                    try
                    {
                        _Copy_File(odi.FullName + "/" + item, ddi.FullName + "/" + item);
                        suc = true;
                    }
                    catch (Exception ex)
                    {
                        //处理因复制文件引发的异常
                        if (File_Operation_Error != null)
                        {
                            File_Operation_Error_Event_Arg e = new File_Operation_Error_Event_Arg();
                            e.ex = new InvalidOperationException("复制 " + odi.FullName + " 时发生错误", ex);
                            File_Operation_Error(ref e);
                            //不重试/取消，则抛出异常，交由上一调用堆栈处理
                            if (e.cancel)
                                throw;
                            if (e.retry == false && e.ignore == true)
                                suc = true;
                        }
                        else
                            throw;
                    }
                } while(!suc);
            }

            //删除文件开始
            foreach (string item in delete_file)
            {
                bool suc = false;
                do
                {
                    try
                    {
                        _Delete_File(ddi.FullName + '/' + item);
                        suc = true;
                    }
                    catch (Exception ex)
                    {
                        //处理因删除文件引发的异常
                        if (File_Operation_Error != null)
                        {
                            File_Operation_Error_Event_Arg e = new File_Operation_Error_Event_Arg();
                            e.ex = new InvalidOperationException("删除 " + odi.FullName + " 时发生错误", ex);
                            File_Operation_Error(ref e);
                            //不重试/取消，则抛出异常，交由上一调用堆栈处理
                            if (e.cancel)
                                throw;
                            if (e.retry == false && e.ignore == true)
                                suc = true;
                        }
                        else
                            throw;
                    }
                } while(!suc);
            }

            //验证+复制文件开始
            foreach (string item in copy_to_be_validated)
            {
                //验证文件
                bool file_skip = false;
                bool suc = false;
                do
                {
                    try
                    {
                        file_skip = _File_Validating(odi.FullName + '/' + item, ddi.FullName + '/' + item, STACHK_flag);
                        suc = true;
                    }
                    catch (Exception ex)
                    {
                        //处理因验证文件引发的异常
                        if (File_Operation_Error != null)
                        {
                            File_Operation_Error_Event_Arg e = new File_Operation_Error_Event_Arg();
                            e.ex = new InvalidOperationException("验证 " + odi.FullName + '/' + item + " 时发生错误", ex);
                            File_Operation_Error(ref e);
                            //不重试/取消，则抛出异常，交由上一调用堆栈处理
                            if (e.cancel)
                                throw;
                            if (e.retry == false && e.ignore == true)
                                suc = true;
                        }
                        else
                            throw;
                    }
                } while(!suc);

                if (file_skip)
                    continue;
                //复制文件
                suc = false;
                do
                {
                    try
                    {
                        _Copy_File(odi.FullName + "/" + item, ddi.FullName + "/" + item);
                        suc = true;
                    }
                    catch (Exception ex)
                    {
                        //处理因复制文件引发的异常
                        if (File_Operation_Error != null)
                        {
                            File_Operation_Error_Event_Arg e = new File_Operation_Error_Event_Arg();
                            e.ex = new InvalidOperationException("复制 " + odi.FullName + " 时发生错误", ex);
                            File_Operation_Error(ref e);
                            //不重试/取消，则抛出异常，交由上一调用堆栈处理
                            if (e.cancel)
                                throw;
                            if (e.retry == false && e.ignore == true)
                                suc = true;
                        }
                        else
                            throw;
                    }
                } while(!suc);
            }

            #endregion

            //函数递归调用同步子文件夹，原理同上，懒得开局部变量了，直接用了，原理跟上面一样，只不过是从文件换成文件夹了，所以就不注释了
            #region Directory_Operation
            //二期新增：文件夹是否隐藏判定
            ls_str_ofi.Clear();
            ls_str_dfi.Clear();
            foreach (DirectoryInfo item in odi.GetDirectories())
            {
                if (!include_hidden_dir_and_file && (item.Attributes & FileAttributes.Hidden) != 0) continue;
                ls_str_ofi.Add(item.Name);
            }
            foreach (DirectoryInfo item in ddi.GetDirectories())
            {
                if (!include_hidden_dir_and_file && (item.Attributes & FileAttributes.Hidden) != 0) continue;
                ls_str_dfi.Add(item.Name);
            }

            copy_file = ls_str_ofi.Except(ls_str_dfi);
            delete_file = ls_str_dfi.Except(ls_str_ofi);
            copy_to_be_validated = ls_str_ofi.Union(ls_str_dfi);

            foreach (string item in copy_file)
                Direct_Sync_File(origin + '/' + item, destination + '/' + item, STACHK_flag, include_hidden_dir_and_file, include_ext, escape_ext);
            foreach (string item in delete_file)
                Directory.Delete(destination + '/' + item, true);
            foreach (string item in copy_to_be_validated)
                Direct_Sync_File(origin + '/' + item, destination + '/' + item, STACHK_flag, include_hidden_dir_and_file, include_ext, escape_ext);
            #endregion
        }
        /// <summary>
        /// 复制文件
        /// </summary>
        /// <param name="origin">源文件夹的根目录</param>
        /// <param name="destination">目标文件夹的根目录</param>
        /// <param name="file_name">文件名</param>
        private void _Copy_File(string origin, string destination)
        {
            try
            {
                //文件数据
                FileInfo ofi = new FileInfo(origin);
                FileInfo dfi = new FileInfo(destination);

                //构造文件复制的事件参数
                File_Copy_Event_Arg e = new File_Copy_Event_Arg();
                e.Current_Position = 0;
                e.File_Length = ofi.Length;
                e.Origin_File_Extension = ofi.Extension;
                e.Destination_File_Extension = dfi.Extension;
                e.Origin_File_Name = ofi.Name;
                e.Destination_File_Name = dfi.Name;
                e.Origin_Full_File_Name = ofi.FullName;
                e.Destination_Full_File_Name = dfi.FullName;
                

                if (File_Begin_Copy_Event != null)
                    File_Begin_Copy_Event(e);

                //创建数据流
                FileStream ofs = ofi.OpenRead(), dfs = dfi.Create();
                byte[] buffer = new byte[STREAM_BUFFER_SIZE];
                int nRead = 0;

                try
                {
                    do
                    {
                        //复制文件内容
                        nRead = ofs.Read(buffer, 0, STREAM_BUFFER_SIZE);
                        e.Current_Position += nRead;
                        dfs.Write(buffer, 0, nRead);

                        if (File_Copying_Event != null)
                            File_Copying_Event(e);

                    } while (nRead != 0);
                }
                finally
                {
                    //关闭数据流
                    try
                    {
                        ofs.Close();
                        dfs.Close();
                    }
                    catch (Exception)
                    {
                    }
                }

                //修改文件属性 -> 注意: 由于文件系统差异，部分属性修改后的误差会变大，所以对比文件时要有一个容差范围
                dfi.CreationTime = new DateTime(((long)(ofi.CreationTime.Ticks / 10000000)) * 10000000);
                dfi.LastAccessTime = new DateTime(((long)(ofi.LastAccessTime.Ticks / 10000000)) * 10000000);
                dfi.LastWriteTime = new DateTime(((long)(ofi.LastWriteTime.Ticks / 10000000)) * 10000000);
                
                if (File_End_Copy_Event != null)
                    File_End_Copy_Event(e);
                }

            catch (Exception)
            {
                
                throw;
            }
        }
        /// <summary>
        /// 函数如其名,就是删除文件
        /// </summary>
        /// <param name="file">要删除的文件</param>
        private void _Delete_File(string file)
        {
            FileInfo fi = new FileInfo(file);

            File_Delete_Event_Arg e = new File_Delete_Event_Arg();
            e.File_Extension = fi.Extension;
            e.Full_File_Name = fi.FullName;
            e.File_Name = fi.Name;

            if (File_Delete_Event != null)
                File_Delete_Event(e);

            fi.Delete();
        }
        /// <summary>
        /// 验证文件是否一致
        /// </summary>
        /// <param name="origin">源文件</param>
        /// <param name="destination">目标文件</param>
        /// <param name="STACHK_flag">验证标识</param>
        /// <returns></returns>
        private bool _File_Validating(string origin, string destination, int STACHK_flag)
        {
            var ofi = new FileInfo(origin);
            var dfi = new FileInfo(destination);

            //todo: 文件时间的容差范围(1s)
            if ((STACHK_flag & FLAG_STACHK_CREATED_TIME) != 0)
            {
                if (!Date_Time_Equation(ofi.CreationTime, dfi.CreationTime, 1000)) return false;
            }

            if ((STACHK_flag & FLAG_STACHK_LAST_MODIFIED_TIME) != 0)
            {
                if (!Date_Time_Equation(ofi.CreationTime, dfi.CreationTime, 2000)) return false;
            }
            
            if ((STACHK_flag & FLAG_STACHK_LAST_ACCESSED_TIME) != 0)
            {
                if (!Date_Time_Equation(ofi.LastAccessTime, dfi.LastAccessTime, 86400000)) return false;
            }
            
            if ((STACHK_flag & FLAG_STACHK_LENGTH) != 0)
            {
                if (ofi.Length != dfi.Length) return false;
            }

            if ((STACHK_flag & FLAG_STACHK_MD5) != 0)
            {
                byte[] omd5 = _Get_File_MD5(ofi.FullName), dmd5 = _Get_File_MD5(dfi.FullName);
                if (omd5 == null || dmd5 == null) throw new InvalidDataException("计算文件MD5值出错");
                if (Others.Hex(omd5) != Others.Hex(dmd5)) return false;
            }

            return true;
        }
        /// <summary>
        /// 比较两个时间的时间差是否小于或等于某个值
        /// </summary>
        /// <param name="t1">第一个时间</param>
        /// <param name="t2">第二个时间</param>
        /// <param name="available_diff_ms">允许的两个时间的时间差（单位:ms）</param>
        /// <returns>若两个时间的时间差小于或等于该值，则返回true，否则返回false</returns>
        private bool Date_Time_Equation(DateTime t1, DateTime t2, int available_diff_ms)
        {
            DateTime max, min;
            if (t1 > t2)
            {
                max = t1; min = t2;
            }
            else
            {
                max = t2; min = t1;
            }

            if ((max - min).TotalMilliseconds > available_diff_ms) return false; else return true;
        }
        /// <summary>
        /// 计算指定文件的MD5值
        /// </summary>
        /// <param name="file">文件名</param>
        /// <returns>文件的MD5值(若出现异常则返回null)</returns>
        private byte[] _Get_File_MD5(string file)
        {
            byte[] ret = null;

            var csp = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] buffer = new byte[STREAM_BUFFER_SIZE];

            csp.Initialize();

            FileInfo fi = new FileInfo(file);
            FileStream fs = null;
            try
            {
                fs = fi.OpenRead();
            }
            catch (Exception)
            {
                return ret;
            }
            //构造MD5计算事件的参数
            File_MD5_Calculate_Event_Arg e = new File_MD5_Calculate_Event_Arg();
            e.Current_Position = 0;
            e.File_Extension = fi.Extension;
            e.Full_File_Name = fi.FullName;
            e.File_Length = fi.Length;
            e.File_Name = fi.Name;

            if (File_MD5_Begin_Calculate_Event != null)
                File_MD5_Begin_Calculate_Event(e);

            //读取文件计算MD5
            try
            {
                fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);

                int nRead = 0;
                do
                {
                    nRead = fs.Read(buffer, 0, STREAM_BUFFER_SIZE);
                    csp.TransformBlock(buffer, 0, nRead, buffer, 0);

                    e.Current_Position += nRead;

                    if (File_MD5_Calculating_Event != null)
                        File_MD5_Calculating_Event(e);

                } while (nRead != 0);

                csp.TransformFinalBlock(buffer, 0, 0);

                ret = csp.Hash;
            }
            finally
            {
                try
                {
                    fs.Close();
                }
                catch (Exception)
                {
                }
                csp.Clear();
            }

            if (File_MD5_End_Calculate_Event != null)
                File_MD5_End_Calculate_Event(e);

            return ret;
        }
        /// <summary>
        /// 删除文件夹
        /// </summary>
        /// <param name="dir">要删除的文件夹</param>
        /// <param name="include_files">是否只删除文件，该标识为true时，目标文件夹及其子文件夹不删除</param>
        /// <param name="include_sub_dir">是否只删除子文件夹，该标识为true时，目标文件夹的文件都不删除</param>
        public void Delete_Directory(string dir, bool include_files = true, bool include_sub_dir = true)
        {
            var di = new DirectoryInfo(dir);
            //删除文件
            if (include_files)
                foreach (FileInfo item in di.GetFiles())
                    item.Delete();
            //删除子目录
            if (include_sub_dir)
                foreach (DirectoryInfo item in di.GetDirectories())
                    Delete_Directory(item.FullName);
            //删除本目录（只有目录为空时才成功）
            di.Delete();
        }
        /// <summary>
        /// 从总路径中获取文件名
        /// </summary>
        /// <param name="full_path">文件的绝对路径</param>
        /// <returns></returns>
        private string _Get_File_Name(string full_path)
        {
            string[] temp = full_path.Split('/');
            return temp.Length > 0 ? temp[temp.Length - 1] : "";
        }
        /// <summary>
        /// 从总路径中获取文件拓展名
        /// </summary>
        /// <param name="full_path">文件的绝对路径</param>
        /// <returns></returns>
        private string _Get_Extension(string full_path)
        {
            string[] temp = _Get_File_Name(full_path).Split('.');
            return temp.Length > 0 ? "." + temp[temp.Length - 1] : "";
        }
    }
}
