using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FolderSync
{
    public partial class repository
    {
        //文件操作


        //文件复制事件参数
        public struct File_Copy_Event_Arg
        {
            public string Origin_File_Name; //不包含路径的文件名
            public string Destination_File_Name;
            public string Origin_File_Extension; //文件拓展名
            public string Destination_File_Extension;
            public string Origin_Full_File_Name; //源文件的绝对路径
            public string Destination_Full_File_Name; //目标文件的绝对路径
            public long File_Length; //文件长度
            public long Current_Position; //当前复制的位置
        }
        public delegate void File_Copy_Event_Handler(File_Copy_Event_Arg e);

        //文件正在复制时引发的事件
        public event File_Copy_Event_Handler File_Copying_Event;
        //文件开始复制时引发的事件
        public event File_Copy_Event_Handler File_Begin_Copy_Event;
        //文件结束复制时引发的事件
        public event File_Copy_Event_Handler File_End_Copy_Event;

        //文件MD5计算事件参数
        public struct File_MD5_Calculate_Event_Arg
        {
            public string File_Name;
            public string File_Extension;
            public string Full_File_Name;
            public long File_Length;
            public long Current_Position;
        }
        public delegate void File_MD5_Calculate_Event_Handler(File_MD5_Calculate_Event_Arg e);
        //正在计算文件MD5引发的事件
        public event File_MD5_Calculate_Event_Handler File_MD5_Calculating_Event;
        //开始计算文件MD5引发的事件
        public event File_MD5_Calculate_Event_Handler File_MD5_Begin_Calculate_Event;
        //结束计算文件MD5引发的事件
        public event File_MD5_Calculate_Event_Handler File_MD5_End_Calculate_Event;

        //文件删除事件参数
        public struct File_Delete_Event_Arg
        {
            public string File_Name;
            public string File_Extension;
            public string Full_File_Name;
        }
        public delegate void File_Delete_Event_Handler(File_Delete_Event_Arg e);
        //文件删除时引发的事件
        public event File_Delete_Event_Handler File_Delete_Event;

        //文件操作异常的事件
        public struct File_Operation_Error_Event_Arg
        {
            public Exception ex; //具体的异常名称
            public bool retry; //是否重试(若cancel==true,该值则被忽略)
            public bool cancel; //是否取消
            public bool ignore; //是否忽略
        }
        public delegate void File_Operation_Error_Event_Handler(ref File_Operation_Error_Event_Arg e);
        public event File_Operation_Error_Event_Handler File_Operation_Error;



        //文件夹操作


        //文件夹创建
        public delegate void Directory_Created_Event_Handler(string directory);
        public event Directory_Created_Event_Handler Directory_Created_Event;

        //文件夹删除
        public delegate void Directory_Deleted_Event_Handler(string directory);
        public event Directory_Deleted_Event_Handler Directory_Deleted_Event;


        //仓库初始化时询问文件夹存在时是删除文件夹内所有文件还是选择其他文件夹
        public delegate void Ask_For_Delete_Directory_Handler(string directory, ref bool delete);
        public event Ask_For_Delete_Directory_Handler Ask_For_Delete_Directory;
    }
}