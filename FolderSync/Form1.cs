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
        public void OutputLog(repo.LogType type, string msg)
        {
            textBox1.Text = "[" + type + "] " + msg + "\r\n";
        }
        public delegate void OutputSafe(string str);
        public void Output(string str)
        {
            textBox1.Text += str + "\r\n";
        }
        public void OutputCopy(string path,long pos,long len)
        {
            this.Invoke(new OutputSafe(Output), path + " pos:" + pos);
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                //repo a = repo.Create("C:/test1", "test");
                repo a = new repo("C:/test1/");
                a.Log += OutputLog;
                a.CopyStatusUpdate += OutputCopy;
                a.Commit_push("D:/xor_data");
                a.Commit_push("F:/cs", "/test/");
                //a.create_local_cache("D:/xor_data/");

            }
            catch (Exception ex)
            {
                Output(ex.ToString());
                //throw;
            }
        }
    }
}
