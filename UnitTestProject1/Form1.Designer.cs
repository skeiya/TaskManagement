﻿namespace UnitTestProject1
{
    partial class Form1
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
            this.gridControl1 = new FreeGridControl.GridControl();
            this.SuspendLayout();
            // 
            // gridControl1
            // 
            this.gridControl1.ColWidths.Add(35);
            this.gridControl1.ColWidths.Add(35);
            this.gridControl1.ColWidths.Add(35);
            this.gridControl1.ColWidths.Add(35);
            this.gridControl1.ColWidths.Add(35);
            this.gridControl1.ColWidths.Add(35);
            this.gridControl1.ColWidths.Add(35);
            this.gridControl1.ColWidths.Add(35);
            this.gridControl1.FixedColCount = 2;
            this.gridControl1.FixedRowCount = 3;
            this.gridControl1.Location = new System.Drawing.Point(83, 52);
            this.gridControl1.Name = "gridControl1";
            this.gridControl1.RowHeights.Add(35);
            this.gridControl1.RowHeights.Add(35);
            this.gridControl1.RowHeights.Add(35);
            this.gridControl1.RowHeights.Add(35);
            this.gridControl1.RowHeights.Add(35);
            this.gridControl1.RowHeights.Add(35);
            this.gridControl1.RowHeights.Add(35);
            this.gridControl1.RowHeights.Add(35);
            this.gridControl1.RowHeights.Add(35);
            this.gridControl1.RowHeights.Add(35);
            this.gridControl1.RowHeights.Add(35);
            this.gridControl1.RowHeights.Add(35);
            this.gridControl1.Size = new System.Drawing.Size(249, 227);
            this.gridControl1.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.gridControl1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private FreeGridControl.GridControl gridControl1;
    }
}