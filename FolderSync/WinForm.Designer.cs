namespace FolderSync
{
    partial class WinForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.commit_pull = new System.Windows.Forms.FolderBrowserDialog();
            this.repo_select = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.repo_delete = new System.Windows.Forms.LinkLabel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.edit_repo_desc = new System.Windows.Forms.TextBox();
            this.commit_file = new System.Windows.Forms.TreeView();
            this.commit_list_rclick = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.复制到本地ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.删除该提交ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.commit_list = new System.Windows.Forms.ComboBox();
            this.commit_push = new System.Windows.Forms.LinkLabel();
            this.repo_desc = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
            this.label4 = new System.Windows.Forms.Label();
            this.status_output = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.commit_list_rclick.SuspendLayout();
            this.toolStripContainer1.ContentPanel.SuspendLayout();
            this.toolStripContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // commit_pull
            // 
            this.commit_pull.Description = "将文件保存到";
            // 
            // repo_select
            // 
            this.repo_select.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.repo_select.FormattingEnabled = true;
            this.repo_select.Items.AddRange(new object[] {
            "<点击创建新的文件仓库>"});
            this.repo_select.Location = new System.Drawing.Point(108, 4);
            this.repo_select.Name = "repo_select";
            this.repo_select.Size = new System.Drawing.Size(398, 20);
            this.repo_select.TabIndex = 0;
            this.repo_select.SelectedIndexChanged += new System.EventHandler(this.repo_select_SelectedIndexChanged2);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 7);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(77, 12);
            this.label1.TabIndex = 1;
            this.label1.Text = "选择一个仓库";
            // 
            // repo_delete
            // 
            this.repo_delete.AutoSize = true;
            this.repo_delete.Enabled = false;
            this.repo_delete.Location = new System.Drawing.Point(512, 7);
            this.repo_delete.Name = "repo_delete";
            this.repo_delete.Size = new System.Drawing.Size(53, 12);
            this.repo_delete.TabIndex = 2;
            this.repo_delete.TabStop = true;
            this.repo_delete.Text = "删除仓库";
            this.repo_delete.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.repo_delete_LinkClicked);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.status_output);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.edit_repo_desc);
            this.groupBox1.Controls.Add(this.commit_file);
            this.groupBox1.Controls.Add(this.commit_list);
            this.groupBox1.Controls.Add(this.commit_push);
            this.groupBox1.Controls.Add(this.repo_desc);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Location = new System.Drawing.Point(9, 30);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(803, 428);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "仓库内容";
            // 
            // edit_repo_desc
            // 
            this.edit_repo_desc.Location = new System.Drawing.Point(65, 365);
            this.edit_repo_desc.Multiline = true;
            this.edit_repo_desc.Name = "edit_repo_desc";
            this.edit_repo_desc.Size = new System.Drawing.Size(732, 39);
            this.edit_repo_desc.TabIndex = 7;
            this.edit_repo_desc.Visible = false;
            this.edit_repo_desc.LostFocus += new System.EventHandler(this.edit_repo_LostFocus);
            // 
            // commit_file
            // 
            this.commit_file.ContextMenuStrip = this.commit_list_rclick;
            this.commit_file.FullRowSelect = true;
            this.commit_file.Location = new System.Drawing.Point(65, 40);
            this.commit_file.Name = "commit_file";
            this.commit_file.Size = new System.Drawing.Size(732, 325);
            this.commit_file.TabIndex = 6;
            // 
            // commit_list_rclick
            // 
            this.commit_list_rclick.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.复制到本地ToolStripMenuItem,
            this.删除该提交ToolStripMenuItem});
            this.commit_list_rclick.Name = "commit_list_rclick";
            this.commit_list_rclick.Size = new System.Drawing.Size(137, 48);
            this.commit_list_rclick.Text = "提交操作";
            // 
            // 复制到本地ToolStripMenuItem
            // 
            this.复制到本地ToolStripMenuItem.Name = "复制到本地ToolStripMenuItem";
            this.复制到本地ToolStripMenuItem.Size = new System.Drawing.Size(136, 22);
            this.复制到本地ToolStripMenuItem.Text = "复制到本地";
            this.复制到本地ToolStripMenuItem.Click += new System.EventHandler(this.复制到本地ToolStripMenuItem_Click);
            // 
            // 删除该提交ToolStripMenuItem
            // 
            this.删除该提交ToolStripMenuItem.Name = "删除该提交ToolStripMenuItem";
            this.删除该提交ToolStripMenuItem.Size = new System.Drawing.Size(136, 22);
            this.删除该提交ToolStripMenuItem.Text = "删除该提交";
            this.删除该提交ToolStripMenuItem.Click += new System.EventHandler(this.删除该提交ToolStripMenuItem_Click);
            // 
            // commit_list
            // 
            this.commit_list.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.commit_list.FormattingEnabled = true;
            this.commit_list.Location = new System.Drawing.Point(65, 14);
            this.commit_list.Name = "commit_list";
            this.commit_list.Size = new System.Drawing.Size(481, 20);
            this.commit_list.TabIndex = 4;
            this.commit_list.SelectedIndexChanged += new System.EventHandler(this.commit_list_SelectedIndexChanged);
            // 
            // commit_push
            // 
            this.commit_push.AutoSize = true;
            this.commit_push.Enabled = false;
            this.commit_push.Location = new System.Drawing.Point(552, 17);
            this.commit_push.Name = "commit_push";
            this.commit_push.Size = new System.Drawing.Size(77, 12);
            this.commit_push.TabIndex = 5;
            this.commit_push.TabStop = true;
            this.commit_push.Text = "创建新的提交";
            this.commit_push.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.commit_push_LinkClicked);
            // 
            // repo_desc
            // 
            this.repo_desc.Location = new System.Drawing.Point(8, 368);
            this.repo_desc.Name = "repo_desc";
            this.repo_desc.Size = new System.Drawing.Size(789, 41);
            this.repo_desc.TabIndex = 4;
            this.repo_desc.Text = "描述:";
            this.repo_desc.DoubleClick += new System.EventHandler(this.repo_desc_DoubleClick);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(8, 40);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(53, 12);
            this.label3.TabIndex = 2;
            this.label3.Text = "文件列表";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 17);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 12);
            this.label2.TabIndex = 0;
            this.label2.Text = "提交清单";
            // 
            // toolStripContainer1
            // 
            // 
            // toolStripContainer1.ContentPanel
            // 
            this.toolStripContainer1.ContentPanel.AutoScroll = true;
            this.toolStripContainer1.ContentPanel.Controls.Add(this.groupBox1);
            this.toolStripContainer1.ContentPanel.Controls.Add(this.repo_delete);
            this.toolStripContainer1.ContentPanel.Controls.Add(this.label1);
            this.toolStripContainer1.ContentPanel.Controls.Add(this.repo_select);
            this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(824, 521);
            this.toolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
            this.toolStripContainer1.Name = "toolStripContainer1";
            this.toolStripContainer1.Size = new System.Drawing.Size(824, 546);
            this.toolStripContainer1.TabIndex = 6;
            this.toolStripContainer1.Text = "toolStripContainer1";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(8, 409);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(35, 12);
            this.label4.TabIndex = 8;
            this.label4.Text = "进度:";
            // 
            // status_output
            // 
            this.status_output.AutoSize = true;
            this.status_output.Location = new System.Drawing.Point(63, 409);
            this.status_output.Name = "status_output";
            this.status_output.Size = new System.Drawing.Size(0, 12);
            this.status_output.TabIndex = 9;
            // 
            // WinForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(824, 546);
            this.Controls.Add(this.toolStripContainer1);
            this.Name = "WinForm";
            this.Text = "Folder Sync v2";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.WinForm_Closing);
            this.Load += new System.EventHandler(this.WinForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.commit_list_rclick.ResumeLayout(false);
            this.toolStripContainer1.ContentPanel.ResumeLayout(false);
            this.toolStripContainer1.ContentPanel.PerformLayout();
            this.toolStripContainer1.ResumeLayout(false);
            this.toolStripContainer1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.FolderBrowserDialog commit_pull;
        private System.Windows.Forms.ComboBox repo_select;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.LinkLabel repo_delete;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label repo_desc;
        private System.Windows.Forms.LinkLabel commit_push;
        private System.Windows.Forms.ToolStripContainer toolStripContainer1;
        private System.Windows.Forms.ComboBox commit_list;
        private System.Windows.Forms.TreeView commit_file;
        private System.Windows.Forms.ContextMenuStrip commit_list_rclick;
        private System.Windows.Forms.ToolStripMenuItem 复制到本地ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 删除该提交ToolStripMenuItem;
        private System.Windows.Forms.TextBox edit_repo_desc;
        private System.Windows.Forms.Label status_output;
        private System.Windows.Forms.Label label4;


    }
}

