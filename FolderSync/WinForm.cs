//Project 2016 - Folder Sync v2
//Author: pandasxd (https://github.com/qhgz2013/FolderSync)
//
//WinForm.cs
//description: Windows窗体UI
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace FolderSync
{
    public partial class WinForm : Form
    {
        public WinForm()
        {
            InitializeComponent();
        }
        public static SortedList<string, string> _repo_list = new SortedList<string,string>();
        private repo _repo;

        //仓库列表

        //读取
        private void WinForm_Load(object sender, EventArgs e)
        {
            if (File.Exists(Application.StartupPath + "\\repo_list"))
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream fs = new FileStream(Application.StartupPath + "\\repo_list", FileMode.Open, FileAccess.Read);
                _repo_list = (SortedList<string, string>)bf.Deserialize(fs);
                fs.Close();

                update_repo_list();
            }

            _repo = null;
        }

        //写入
        private void WinForm_Closing(object sender, FormClosingEventArgs e)
        {
            if (_repo != null)
                _repo.Dispose();

            BinaryFormatter bf = new BinaryFormatter();
            FileStream fs = new FileStream(Application.StartupPath + "\\repo_list", FileMode.Create, FileAccess.Write);
            bf.Serialize(fs, _repo_list);
            fs.Close();

        }

        //更新list到combo box
        private void update_repo_list()
        {
            repo_select.Items.Clear();
            foreach (KeyValuePair<string, string> item in _repo_list)
            {
                repo_select.Items.Add(item.Key);
            }
            repo_select.Items.Add("<点击创建新的仓库>");
        }
        //创建仓库的回调函数
        public void repo_create_callback(string name, string addr)
        {
            _repo_list.Add(name, repo.format_addr(addr));
            update_repo_list();
            repo_select.SelectedIndex = repo_select.Items.IndexOf(name);
        }
        //选择仓库
        private void repo_select_SelectedIndexChanged2(object sender, EventArgs e)
        {
            if (repo_select.SelectedIndex == -1)
            {
                repo_delete.Enabled = false;
                return;
            }
            if (repo_select.SelectedIndex == repo_select.Items.Count - 1)
            {
                clear_repo_ui();
                WinForm_repoCreate widget = new WinForm_repoCreate();
                widget.ShowDialog(this);
            }
            else
            {
                repo_select.Enabled = false;
                load_repo();
                repo_select.Enabled = true;
            }
        }
        //clear ui
        private void clear_repo_ui()
        {
            if (_repo != null)
                _repo.Dispose();
            _repo = null;
            repo_select.SelectedIndex = -1;
            repo_delete.Enabled = false;
            commit_push.Enabled = false;
            commit_list.Items.Clear();
            commit_file.Nodes.Clear();
            repo_desc.Text = "描述:";
        }
        //删除仓库
        private void repo_delete_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (repo_select.SelectedIndex == -1)
                return;
            string name = (string)repo_select.Items[repo_select.SelectedIndex];
            if (MessageBox.Show("从该列表中移除?", "注意", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
                return;

            if (_repo != null)
                _repo.Dispose();
            _repo = null;

            if (MessageBox.Show("同时移除本地文件?\n注意：移除本地文件后无法恢复","警告",MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
            {
                if (Directory.Exists(_repo_list[name]))
                    Directory.Delete(_repo_list[name], true);
            }
            _repo_list.Remove(name);
            update_repo_list();

            clear_repo_ui();
        }

        //读取仓库
        private void load_repo()
        {
            if (repo_select.SelectedIndex == -1)
                return;
            string name = (string)repo_select.Items[repo_select.SelectedIndex];

            if (_repo != null)
                _repo.Dispose();

            _repo = new repo(_repo_list[name]);

            _repo.Calculating_MD5 += this.Calculate_MD5_Callback;
            _repo.Copy_Status += this.Copy_Status_Callback;

            repo_desc.Text = "描述:" + _repo.Description;
            load_commit_list();
            repo_delete.Enabled = true;
            commit_push.Enabled = true;
        }
        private void load_commit_list()
        {
            if (_repo == null)
                return;
            List<repo.commit_list_data> _commit_list = _repo.Get_commit_list();
            commit_list.Items.Clear();
            commit_file.Nodes.Clear();
            if (_commit_list.Count == 0)
                return;
            foreach (repo.commit_list_data item in _commit_list)
            {
                commit_list.Items.Insert(0, item.title + " [" + util.hex(item.commit_SHA).ToLower() + "]");
            }
            commit_list.SelectedIndex = 0;
        }

        private void load_commit_file()
        {
            if (_repo == null)
                return; 
            commit_file.Nodes.Clear();
            if (commit_list.SelectedIndex == -1)
                return;
            List<repo.commit_full_data> _commit_file = _repo.Get_commit_full_file_list(_repo.Commit_Count - commit_list.SelectedIndex - 1);
            commit_file.Nodes.Add(_create_tree_node_by_file_list(_commit_file));
            commit_file.Nodes[0].Expand();
        }
        //创建提交
        public void commit_push_callback(string local_addr, string title, string desc, string root, bool force)
        {
            _repo.Commit_push(local_addr, title, desc, root, force);
            load_commit_list();
        }
        private void commit_push_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            WinForm_commitPush form = new WinForm_commitPush();
            form.ShowDialog(this);
        }
        //获取提交的文件列表
        private void commit_list_SelectedIndexChanged(object sender, EventArgs e)
        {
            commit_file.Nodes.Clear();
            if (commit_list.SelectedIndex == -1)
                return;
            load_commit_file();
        }


        #region File list to tree node display
        private TreeNode _create_tree_node_by_file_list(List<repo.commit_full_data> list)
        {
            TreeNode ret = new TreeNode("<根目录>");
            ret.Name = "<root>";
            TreeNode temp;
            HashSet<string> a = new HashSet<string>();
            a.Add("");

            foreach (repo.commit_full_data item in list)
            {
                temp = ret;
                string[] data = item.addr.Split('\\');
                string full_addr = "";
                for (int i = 1; i < data.Length - 1; i++)
                {
                    full_addr += "\\" + data[i];
                    if(!a.Contains(full_addr))
                    {
                        a.Add(full_addr);
                        TreeNode InsNode = new TreeNode(data[i]);
                        InsNode.Name = data[i];
                        temp.Nodes.Add(InsNode);
                    }
                    temp = temp.Nodes[data[i]];
                }
                temp.Nodes.Add(data[data.Length - 1]);
            }

            return ret;
        }
        #endregion

        //删除提交
        private void 删除该提交ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_repo == null || commit_list.SelectedIndex == -1)
                return;

            _repo.Commit_delete(_repo.Commit_Count - commit_list.SelectedIndex - 1);
            load_commit_list();
        }
        //修改仓库描述
        private void repo_desc_DoubleClick(object sender, EventArgs e)
        {
            if (_repo == null)
                return;
            edit_repo_desc.Text = repo_desc.Text.Substring(3);
            edit_repo_desc.Visible = true;
            repo_desc.Visible = false;
            edit_repo_desc.Focus();
        }
        private void edit_repo_LostFocus(object sender, EventArgs e)
        {
            if (_repo == null)
                return;
            edit_repo_desc.Visible = false;
            repo_desc.Visible = true;
            edit_repo_desc.Text = edit_repo_desc.Text.Replace("\r", "").Replace("\n", "");
            repo_desc.Text = "描述:" + edit_repo_desc.Text;
            _repo.Description = edit_repo_desc.Text;
        }
        //复制文件
        private void 复制到本地ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_repo == null || commit_list.SelectedIndex == -1)
                return;
            if (commit_pull.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            _repo.Commit_pull(commit_pull.SelectedPath, util.hex(_repo.Get_commit_data(_repo.Commit_Count - commit_list.SelectedIndex - 1).commit_SHA).ToLower());
        }

        //仓库事件调用函数
        private delegate void status_output_safe(string str);
        private void _status_output(string str)
        {
            status_output.Text = str;
        }
        private void Copy_Status_Callback(string addr, long pos, long len)
        {
            Application.DoEvents();
            this.Invoke(new status_output_safe(_status_output), "复制文件: " + addr + "(" + pos + "B/" + len + "B)");
        }
        private void Calculate_MD5_Callback(string addr, long pos, long len)
        {
            Application.DoEvents();
            this.Invoke(new status_output_safe(_status_output), "计算文件MD5: " + addr + "(" + pos + "B/" + len + "B)");
        }
    }
}
