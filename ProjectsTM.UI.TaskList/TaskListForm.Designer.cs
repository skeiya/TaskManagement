﻿namespace ProjectsTM.UI.TaskList
{
    partial class TaskListForm
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
            this.gridControl1 = new ProjectsTM.UI.TaskList.TaskListGrid();
            this.comboBoxPattern = new System.Windows.Forms.ComboBox();
            this.buttonUpdate = new System.Windows.Forms.Button();
            this.radioButtonFilter = new System.Windows.Forms.RadioButton();
            this.radioButtonAudit = new System.Windows.Forms.RadioButton();
            this.labelSum = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // gridControl1
            // 
            this.gridControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gridControl1.FixedColCount = 0;
            this.gridControl1.FixedRowCount = 0;
            this.gridControl1.Location = new System.Drawing.Point(12, 44);
            this.gridControl1.LockUpdate = true;
            this.gridControl1.Name = "gridControl1";
            this.gridControl1.Size = new System.Drawing.Size(726, 239);
            this.gridControl1.TabIndex = 2;
            // 
            // comboBoxPattern
            // 
            this.comboBoxPattern.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxPattern.FormattingEnabled = true;
            this.comboBoxPattern.Location = new System.Drawing.Point(137, 12);
            this.comboBoxPattern.Name = "comboBoxPattern";
            this.comboBoxPattern.Size = new System.Drawing.Size(520, 20);
            this.comboBoxPattern.TabIndex = 0;
            this.comboBoxPattern.DropDown += new System.EventHandler(this.comboBoxPattern_DropDown);
            // 
            // buttonUpdate
            // 
            this.buttonUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonUpdate.Location = new System.Drawing.Point(663, 10);
            this.buttonUpdate.Name = "buttonUpdate";
            this.buttonUpdate.Size = new System.Drawing.Size(75, 23);
            this.buttonUpdate.TabIndex = 1;
            this.buttonUpdate.Text = "更新";
            this.buttonUpdate.UseVisualStyleBackColor = true;
            this.buttonUpdate.Click += new System.EventHandler(this.buttonUpdate_Click);
            // 
            // radioButtonFilter
            // 
            this.radioButtonFilter.AutoSize = true;
            this.radioButtonFilter.Checked = true;
            this.radioButtonFilter.Location = new System.Drawing.Point(12, 13);
            this.radioButtonFilter.Name = "radioButtonFilter";
            this.radioButtonFilter.Size = new System.Drawing.Size(66, 16);
            this.radioButtonFilter.TabIndex = 3;
            this.radioButtonFilter.TabStop = true;
            this.radioButtonFilter.Text = "フィルター";
            this.radioButtonFilter.UseVisualStyleBackColor = true;
            this.radioButtonFilter.CheckedChanged += new System.EventHandler(this.radioButtonFilter_CheckedChanged);
            // 
            // radioButtonAudit
            // 
            this.radioButtonAudit.AutoSize = true;
            this.radioButtonAudit.Location = new System.Drawing.Point(84, 13);
            this.radioButtonAudit.Name = "radioButtonAudit";
            this.radioButtonAudit.Size = new System.Drawing.Size(47, 16);
            this.radioButtonAudit.TabIndex = 4;
            this.radioButtonAudit.Text = "監査";
            this.radioButtonAudit.UseVisualStyleBackColor = true;
            this.radioButtonAudit.CheckedChanged += new System.EventHandler(this.radioButtonAudit_CheckedChanged);
            // 
            // labelSum
            // 
            this.labelSum.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelSum.AutoSize = true;
            this.labelSum.Location = new System.Drawing.Point(12, 288);
            this.labelSum.Name = "labelSum";
            this.labelSum.Size = new System.Drawing.Size(29, 12);
            this.labelSum.TabIndex = 5;
            this.labelSum.Text = "合計";
            // 
            // TaskListForm
            // 
            this.AcceptButton = this.buttonUpdate;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(750, 307);
            this.Controls.Add(this.labelSum);
            this.Controls.Add(this.radioButtonAudit);
            this.Controls.Add(this.radioButtonFilter);
            this.Controls.Add(this.buttonUpdate);
            this.Controls.Add(this.comboBoxPattern);
            this.Controls.Add(this.gridControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "TaskListForm";
            this.Text = "タスクリスト";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private TaskListGrid gridControl1;
        private System.Windows.Forms.ComboBox comboBoxPattern;
        private System.Windows.Forms.Button buttonUpdate;
        private System.Windows.Forms.RadioButton radioButtonFilter;
        private System.Windows.Forms.RadioButton radioButtonAudit;
        private System.Windows.Forms.Label labelSum;
    }
}