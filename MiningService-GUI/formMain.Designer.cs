namespace MiningService
{
    partial class FormMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.menu = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.MiningServiceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.runToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.startServiceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stopServiceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.installServiceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.uninstallServiceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.configurationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.minerConfigurationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ignoredProgramsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.checkedListSettings = new System.Windows.Forms.CheckedListBox();
            this.menu.SuspendLayout();
            this.SuspendLayout();
            // 
            // menu
            // 
            this.menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.MiningServiceToolStripMenuItem,
            this.configurationToolStripMenuItem});
            this.menu.Location = new System.Drawing.Point(0, 0);
            this.menu.Name = "menu";
            this.menu.Size = new System.Drawing.Size(421, 24);
            this.menu.TabIndex = 0;
            this.menu.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.toolStripMenuItem1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // loadToolStripMenuItem
            // 
            this.loadToolStripMenuItem.Name = "loadToolStripMenuItem";
            this.loadToolStripMenuItem.Size = new System.Drawing.Size(100, 22);
            this.loadToolStripMenuItem.Text = "&Load";
            this.loadToolStripMenuItem.Click += new System.EventHandler(this.loadToolStripMenuItem_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Enabled = false;
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(100, 22);
            this.saveToolStripMenuItem.Text = "&Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(97, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(100, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // MiningServiceToolStripMenuItem
            // 
            this.MiningServiceToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.runToolStripMenuItem,
            this.startServiceToolStripMenuItem,
            this.stopServiceToolStripMenuItem,
            this.installServiceToolStripMenuItem,
            this.uninstallServiceToolStripMenuItem});
            this.MiningServiceToolStripMenuItem.Name = "MiningServiceToolStripMenuItem";
            this.MiningServiceToolStripMenuItem.Size = new System.Drawing.Size(94, 20);
            this.MiningServiceToolStripMenuItem.Text = "MiningService";
            // 
            // runToolStripMenuItem
            // 
            this.runToolStripMenuItem.Name = "runToolStripMenuItem";
            this.runToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
            this.runToolStripMenuItem.Text = "Run";
            this.runToolStripMenuItem.Click += new System.EventHandler(this.runToolStripMenuItem_Click);
            // 
            // startServiceToolStripMenuItem
            // 
            this.startServiceToolStripMenuItem.Name = "startServiceToolStripMenuItem";
            this.startServiceToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
            this.startServiceToolStripMenuItem.Text = "Start Service";
            this.startServiceToolStripMenuItem.Click += new System.EventHandler(this.startServiceToolStripMenuItem_Click);
            // 
            // stopServiceToolStripMenuItem
            // 
            this.stopServiceToolStripMenuItem.Name = "stopServiceToolStripMenuItem";
            this.stopServiceToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
            this.stopServiceToolStripMenuItem.Text = "Stop Service";
            this.stopServiceToolStripMenuItem.Click += new System.EventHandler(this.stopServiceToolStripMenuItem_Click);
            // 
            // installServiceToolStripMenuItem
            // 
            this.installServiceToolStripMenuItem.Name = "installServiceToolStripMenuItem";
            this.installServiceToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
            this.installServiceToolStripMenuItem.Text = "Install Service";
            this.installServiceToolStripMenuItem.Click += new System.EventHandler(this.installServiceToolStripMenuItem_Click);
            // 
            // uninstallServiceToolStripMenuItem
            // 
            this.uninstallServiceToolStripMenuItem.Name = "uninstallServiceToolStripMenuItem";
            this.uninstallServiceToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
            this.uninstallServiceToolStripMenuItem.Text = "Uninstall Service";
            this.uninstallServiceToolStripMenuItem.Click += new System.EventHandler(this.uninstallServiceToolStripMenuItem_Click);
            // 
            // configurationToolStripMenuItem
            // 
            this.configurationToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.minerConfigurationToolStripMenuItem,
            this.ignoredProgramsToolStripMenuItem});
            this.configurationToolStripMenuItem.Name = "configurationToolStripMenuItem";
            this.configurationToolStripMenuItem.Size = new System.Drawing.Size(93, 20);
            this.configurationToolStripMenuItem.Text = "&Configuration";
            // 
            // minerConfigurationToolStripMenuItem
            // 
            this.minerConfigurationToolStripMenuItem.Name = "minerConfigurationToolStripMenuItem";
            this.minerConfigurationToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.minerConfigurationToolStripMenuItem.Text = "&Miner Configuration";
            this.minerConfigurationToolStripMenuItem.Click += new System.EventHandler(this.minerConfigurationToolStripMenuItem_Click);
            // 
            // ignoredProgramsToolStripMenuItem
            // 
            this.ignoredProgramsToolStripMenuItem.Name = "ignoredProgramsToolStripMenuItem";
            this.ignoredProgramsToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.ignoredProgramsToolStripMenuItem.Text = "&Ignored Programs";
            this.ignoredProgramsToolStripMenuItem.Click += new System.EventHandler(this.ignoredProgramsToolStripMenuItem_Click);
            // 
            // checkedListSettings
            // 
            this.checkedListSettings.CheckOnClick = true;
            this.checkedListSettings.FormattingEnabled = true;
            this.checkedListSettings.Location = new System.Drawing.Point(12, 27);
            this.checkedListSettings.Name = "checkedListSettings";
            this.checkedListSettings.Size = new System.Drawing.Size(196, 169);
            this.checkedListSettings.TabIndex = 5;
            this.checkedListSettings.Visible = false;
            this.checkedListSettings.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.ItemWasUpdated);
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(421, 360);
            this.Controls.Add(this.checkedListSettings);
            this.Controls.Add(this.menu);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MainMenuStrip = this.menu;
            this.MaximizeBox = false;
            this.Name = "FormMain";
            this.Text = "MiningService Configuration GUI";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.formMain_FormClosing);
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.menu.ResumeLayout(false);
            this.menu.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menu;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem MiningServiceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem runToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem startServiceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stopServiceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem uninstallServiceToolStripMenuItem;
        private System.Windows.Forms.CheckedListBox checkedListSettings;
        private System.Windows.Forms.ToolStripMenuItem configurationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem minerConfigurationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ignoredProgramsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem installServiceToolStripMenuItem;
    }
}

