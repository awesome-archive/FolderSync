using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace FolderSync
{
    public partial class WinForm_commitPush : Form
    {
        public WinForm_commitPush()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox1.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                Regex reg = new Regex(@"^(?<fpath>([a-zA-Z]:\\)([\s\.\-\w]+\\)*)");
                Regex reg2 = new Regex(@"([\s\.\-\w)]+[\\/])*");
                if (string.IsNullOrEmpty(textBox1.Text))
                {
                    MessageBox.Show("名称不能为空!");
                    return;
                }
                if (!reg.Match(textBox1.Text).Success)
                {
                    MessageBox.Show("不是一个合法的文件夹名");
                    return;
                }
                if (!reg2.Match(textBox2.Text).Success)
                {
                    MessageBox.Show("不是合法的储存路径");
                    return;
                }


                ((WinForm)Owner).commit_push_callback(textBox1.Text, textBox3.Text, textBox4.Text, textBox2.Text, checkBox1.Checked);
                this.Close();
            }
            catch (Exception ex)
            {

                //throw;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
