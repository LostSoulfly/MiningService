namespace MiningService
{
    partial class MinerEditor
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
            this.components = new System.ComponentModel.Container();
            this.buttonForward = new System.Windows.Forms.Button();
            this.buttonBack = new System.Windows.Forms.Button();
            this.buttonAdd = new System.Windows.Forms.Button();
            this.buttonDelete = new System.Windows.Forms.Button();
            this.comboMiners = new System.Windows.Forms.ComboBox();
            this.groupMiner = new System.Windows.Forms.GroupBox();
            this.checkMinerDisabled = new System.Windows.Forms.CheckBox();
            this.checkMineNotIdle = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textActive = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonOpen = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.textIdleArgs = new System.Windows.Forms.TextBox();
            this.textExecutable = new System.Windows.Forms.TextBox();
            this.buttonSave = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.groupMiner.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonForward
            // 
            this.buttonForward.AutoSize = true;
            this.buttonForward.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonForward.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonForward.Location = new System.Drawing.Point(486, 10);
            this.buttonForward.Name = "buttonForward";
            this.buttonForward.Size = new System.Drawing.Size(32, 30);
            this.buttonForward.TabIndex = 0;
            this.buttonForward.Text = ">";
            this.buttonForward.UseVisualStyleBackColor = true;
            this.buttonForward.Click += new System.EventHandler(this.buttonForward_Click);
            // 
            // buttonBack
            // 
            this.buttonBack.AutoSize = true;
            this.buttonBack.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonBack.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonBack.Location = new System.Drawing.Point(373, 10);
            this.buttonBack.Name = "buttonBack";
            this.buttonBack.Size = new System.Drawing.Size(32, 30);
            this.buttonBack.TabIndex = 1;
            this.buttonBack.Text = "<";
            this.buttonBack.UseVisualStyleBackColor = true;
            this.buttonBack.Click += new System.EventHandler(this.buttonBack_Click);
            // 
            // buttonAdd
            // 
            this.buttonAdd.AutoSize = true;
            this.buttonAdd.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonAdd.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonAdd.Location = new System.Drawing.Point(411, 10);
            this.buttonAdd.Name = "buttonAdd";
            this.buttonAdd.Size = new System.Drawing.Size(32, 30);
            this.buttonAdd.TabIndex = 3;
            this.buttonAdd.Text = "+";
            this.buttonAdd.UseVisualStyleBackColor = true;
            this.buttonAdd.Click += new System.EventHandler(this.buttonAdd_Click);
            // 
            // buttonDelete
            // 
            this.buttonDelete.AutoSize = true;
            this.buttonDelete.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonDelete.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonDelete.Location = new System.Drawing.Point(447, 10);
            this.buttonDelete.Name = "buttonDelete";
            this.buttonDelete.Size = new System.Drawing.Size(33, 30);
            this.buttonDelete.TabIndex = 2;
            this.buttonDelete.Text = "--";
            this.buttonDelete.UseVisualStyleBackColor = true;
            this.buttonDelete.Click += new System.EventHandler(this.buttonDelete_Click);
            // 
            // comboMiners
            // 
            this.comboMiners.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboMiners.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.comboMiners.FormattingEnabled = true;
            this.comboMiners.Items.AddRange(new object[] {
            "CPU Miners",
            "GPU Miners"});
            this.comboMiners.Location = new System.Drawing.Point(13, 12);
            this.comboMiners.Name = "comboMiners";
            this.comboMiners.Size = new System.Drawing.Size(153, 28);
            this.comboMiners.TabIndex = 4;
            this.comboMiners.SelectedIndexChanged += new System.EventHandler(this.comboMiners_SelectedIndexChanged);
            // 
            // groupMiner
            // 
            this.groupMiner.Controls.Add(this.checkMinerDisabled);
            this.groupMiner.Controls.Add(this.checkMineNotIdle);
            this.groupMiner.Controls.Add(this.label3);
            this.groupMiner.Controls.Add(this.textActive);
            this.groupMiner.Controls.Add(this.label2);
            this.groupMiner.Controls.Add(this.buttonOpen);
            this.groupMiner.Controls.Add(this.label1);
            this.groupMiner.Controls.Add(this.textIdleArgs);
            this.groupMiner.Controls.Add(this.textExecutable);
            this.groupMiner.Location = new System.Drawing.Point(13, 47);
            this.groupMiner.Name = "groupMiner";
            this.groupMiner.Size = new System.Drawing.Size(505, 160);
            this.groupMiner.TabIndex = 10;
            this.groupMiner.TabStop = false;
            this.groupMiner.Text = "Miner";
            // 
            // checkMinerDisabled
            // 
            this.checkMinerDisabled.AutoSize = true;
            this.checkMinerDisabled.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.checkMinerDisabled.Location = new System.Drawing.Point(372, 37);
            this.checkMinerDisabled.Name = "checkMinerDisabled";
            this.checkMinerDisabled.Size = new System.Drawing.Size(127, 17);
            this.checkMinerDisabled.TabIndex = 20;
            this.checkMinerDisabled.Text = "This Miner is disabled";
            this.checkMinerDisabled.UseVisualStyleBackColor = true;
            // 
            // checkMineNotIdle
            // 
            this.checkMineNotIdle.AutoSize = true;
            this.checkMineNotIdle.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.checkMineNotIdle.Location = new System.Drawing.Point(264, 12);
            this.checkMineNotIdle.Name = "checkMineNotIdle";
            this.checkMineNotIdle.Size = new System.Drawing.Size(235, 17);
            this.checkMineNotIdle.TabIndex = 19;
            this.checkMineNotIdle.Text = "This Miner runs even if computer is not IDLE";
            this.checkMineNotIdle.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 114);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(119, 13);
            this.label3.TabIndex = 17;
            this.label3.Text = "Active Miner Arguments";
            // 
            // textActive
            // 
            this.textActive.Location = new System.Drawing.Point(9, 130);
            this.textActive.Name = "textActive";
            this.textActive.Size = new System.Drawing.Size(489, 20);
            this.textActive.TabIndex = 16;
            this.textActive.Enter += new System.EventHandler(this.textActive_Enter);
            this.textActive.MouseEnter += new System.EventHandler(this.textActive_MouseEnter);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 64);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(106, 13);
            this.label2.TabIndex = 15;
            this.label2.Text = "Idle Miner Arguments";
            // 
            // buttonOpen
            // 
            this.buttonOpen.AutoSize = true;
            this.buttonOpen.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonOpen.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonOpen.Location = new System.Drawing.Point(267, 33);
            this.buttonOpen.Name = "buttonOpen";
            this.buttonOpen.Size = new System.Drawing.Size(33, 22);
            this.buttonOpen.TabIndex = 14;
            this.buttonOpen.Text = "...";
            this.buttonOpen.UseVisualStyleBackColor = true;
            this.buttonOpen.Click += new System.EventHandler(this.buttonOpen_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(117, 13);
            this.label1.TabIndex = 13;
            this.label1.Text = "Miner Executable Path:";
            // 
            // textIdleArgs
            // 
            this.textIdleArgs.Location = new System.Drawing.Point(9, 80);
            this.textIdleArgs.Name = "textIdleArgs";
            this.textIdleArgs.Size = new System.Drawing.Size(489, 20);
            this.textIdleArgs.TabIndex = 9;
            this.textIdleArgs.Enter += new System.EventHandler(this.textIdleArgs_Enter);
            this.textIdleArgs.MouseEnter += new System.EventHandler(this.textIdleArgs_MouseEnter);
            // 
            // textExecutable
            // 
            this.textExecutable.Location = new System.Drawing.Point(9, 34);
            this.textExecutable.Name = "textExecutable";
            this.textExecutable.Size = new System.Drawing.Size(252, 20);
            this.textExecutable.TabIndex = 8;
            this.textExecutable.Enter += new System.EventHandler(this.textExecutable_Enter);
            this.textExecutable.MouseEnter += new System.EventHandler(this.textExecutable_MouseEnter);
            // 
            // buttonSave
            // 
            this.buttonSave.DialogResult = System.Windows.Forms.DialogResult.Yes;
            this.buttonSave.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonSave.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonSave.Location = new System.Drawing.Point(174, 12);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(68, 30);
            this.buttonSave.TabIndex = 11;
            this.buttonSave.Text = "Save";
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.No;
            this.buttonCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonCancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonCancel.Location = new System.Drawing.Point(248, 12);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(68, 30);
            this.buttonCancel.TabIndex = 12;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // MinerEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(530, 215);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonSave);
            this.Controls.Add(this.groupMiner);
            this.Controls.Add(this.comboMiners);
            this.Controls.Add(this.buttonAdd);
            this.Controls.Add(this.buttonDelete);
            this.Controls.Add(this.buttonBack);
            this.Controls.Add(this.buttonForward);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MinimizeBox = false;
            this.Name = "MinerEditor";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "MinerEditor";
            this.Load += new System.EventHandler(this.MinerEditor_Load);
            this.groupMiner.ResumeLayout(false);
            this.groupMiner.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonForward;
        private System.Windows.Forms.Button buttonBack;
        private System.Windows.Forms.Button buttonAdd;
        private System.Windows.Forms.Button buttonDelete;
        private System.Windows.Forms.ComboBox comboMiners;
        private System.Windows.Forms.GroupBox groupMiner;
        private System.Windows.Forms.CheckBox checkMinerDisabled;
        private System.Windows.Forms.CheckBox checkMineNotIdle;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textActive;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button buttonOpen;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textIdleArgs;
        private System.Windows.Forms.TextBox textExecutable;
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.ToolTip toolTip;
    }
}