using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.IO;
using System.Text.RegularExpressions;
namespace FolderSync
{
    public partial class WinForm_repoCreate : Form
    {
        public WinForm_repoCreate()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (repo_create.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                textBox2.Text = repo_create.SelectedPath;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                Regex reg = new Regex(@"^(?<fpath>([a-zA-Z]:\\)([\s\.\-\w]+\\)*)");
                if (string.IsNullOrEmpty( textBox1.Text))
                {
                    MessageBox.Show("名称不能为空!");
                    return;
                }
                if (reg.Match(textBox2.Text).Success)
                {
                    //WinForm._repo_list.Add(textBox1.Text, textBox2.Text);
                    repo.Create(textBox2.Text, textBox3.Text, false).Dispose();
                    ((WinForm)Owner).repo_create_callback(textBox1.Text, textBox2.Text);
                    this.Close();
                }
                else
                {
                    MessageBox.Show("不是一个合法的文件夹名");
                    return;
                }


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
