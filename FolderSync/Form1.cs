using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FolderSync
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private repository repo;
        private void Form1_Load(object sender, EventArgs e)
        {
            //test

            repo = new repository();

            repo.Directory_Created_Event += new repository.Directory_Created_Event_Handler(on_dir_created);
            repo.Directory_Deleted_Event += new repository.Directory_Deleted_Event_Handler(on_dir_deleted);
            repo.File_Begin_Copy_Event += new repository.File_Copy_Event_Handler(on_file_copy_start);
            repo.File_Copying_Event += new repository.File_Copy_Event_Handler(on_file_copying);
            repo.File_Delete_Event += new repository.File_Delete_Event_Handler(on_file_delete_start);
            repo.File_MD5_Begin_Calculate_Event += new repository.File_MD5_Calculate_Event_Handler(on_file_md5_calc_start);
            repo.File_MD5_Calculating_Event += new repository.File_MD5_Calculate_Event_Handler(on_file_md5_calcing);
            repo.File_Operation_Error += new repository.File_Operation_Error_Event_Handler(on_file_error);

            doEventTime = DateTime.Now;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            /*
            if (folderBrowserDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                label1.Text = "src:";
                label2.Text = "dst:";
                return;
            }

            if (folderBrowserDialog2.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                label1.Text = "src:";
                label2.Text = "dst:";
                return;
            }

            label1.Text = "src:" + folderBrowserDialog1.SelectedPath;
            label2.Text = "dst:" + folderBrowserDialog2.SelectedPath;

            repo.Direct_Sync_File(folderBrowserDialog1.SelectedPath, folderBrowserDialog2.SelectedPath);
             */

            //repo.Direct_Sync_File("F:/pixiv", "G:/pixiv", repository.FLAG_STACHK_DIRECT_SYNC_DEFAULT, false, new List<string>() { ".png" });
            repo.Create_repository("D:/test1");
            repo.Open("D:/test1");
            repo.Dir_Create("/test");
        }

        private void on_file_copy_start(repository.File_Copy_Event_Arg e)
        {
            var lvi = new ListViewItem("复制文件: " + e.Origin_Full_File_Name + " -> " + e.Destination_Full_File_Name);
            progressBar1.Value = 0;
            progressBar1.Maximum = (int)e.File_Length;
            //listView1.Items.Add(lvi);

            label3.Text = "cur: 复制文件: " + e.Origin_File_Name + " [0 / " + e.File_Length + "]";

            call_doevents();
        }
        private void on_file_copying(repository.File_Copy_Event_Arg e)
        {
            progressBar1.Value = (int)e.Current_Position;
            label3.Text = "cur: 复制文件: " + e.Origin_File_Name + " [" + e.Current_Position + " / " + e.File_Length + "]";

            call_doevents();
        }
        private void on_file_delete_start(repository.File_Delete_Event_Arg e)
        {
            progressBar1.Value = 0;
            progressBar1.Maximum = 0;
            label3.Text = "cur: 删除文件: " + e.File_Name;
            var lvi = new ListViewItem("删除文件: " + e.Full_File_Name);
            //listView1.Items.Add(lvi);

            call_doevents();
        }

        private void on_dir_created(string dir)
        {
            progressBar1.Value = 0;
            progressBar1.Maximum = 0;
            label3.Text = "cur: 创建文件夹: " + dir;
            var lvi = new ListViewItem("创建文件夹: " + dir);
            listView1.Items.Add(lvi);

            call_doevents();

        }
        private void on_dir_deleted(string dir)
        {
            progressBar1.Value = 0;
            progressBar1.Maximum = 0;
            label3.Text = "cur: 删除文件夹: " + dir;
            var lvi = new ListViewItem("删除文件夹: " + dir);
            //listView1.Items.Add(lvi);

            call_doevents();

        }
        private void on_file_error(ref repository.File_Operation_Error_Event_Arg e)
        {
            var result = MessageBox.Show(e.ex.ToString(), "出错啦", MessageBoxButtons.AbortRetryIgnore);

            if (result == System.Windows.Forms.DialogResult.Retry) e.retry = true;
            if (result == System.Windows.Forms.DialogResult.Abort) e.cancel = true;
            if (result == System.Windows.Forms.DialogResult.Ignore) e.ignore = true;
        }
        private void on_file_md5_calc_start(repository.File_MD5_Calculate_Event_Arg e)
        {

            var lvi = new ListViewItem("计算文件MD5: " + e.Full_File_Name);
            progressBar1.Value = 0;
            progressBar1.Maximum = (int)e.File_Length;
            //listView1.Items.Add(lvi);

            label3.Text = "cur: 计算文件MD5: " + e.File_Name + " [0 / " + e.File_Length + "]";

            call_doevents();
        }
        private void on_file_md5_calcing(repository.File_MD5_Calculate_Event_Arg e)
        {
            progressBar1.Value = (int)e.Current_Position;
            label3.Text = "cur: 计算文件MD5: " + e.File_Name + " [" + e.Current_Position + " / " + e.File_Length + "]";

            call_doevents();

        }

        private DateTime doEventTime;
        private void call_doevents()
        {
            if (doEventTime < DateTime.Now)
            {
                doEventTime = doEventTime.AddMilliseconds(100);
                Application.DoEvents();
            }
        }
    }
}
