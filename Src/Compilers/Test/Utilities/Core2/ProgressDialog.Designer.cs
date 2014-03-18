
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis.Test.Utilities
{
    partial class ProgressDialog : Form
    {
        [DebuggerNonUserCode()]
        private void InitializeComponent()
        {
            this.Label1 = new Label();
            this.Label2 = new Label();
            this.ProgressBar1 = new ProgressBar();
            this.SuspendLayout();
            //
            //Label1
            //
            this.Label1.AutoSize = true;
            this.Label1.Location = new Point(12, 50);
            this.Label1.Name = "Label1";
            this.Label1.Size = new Size(60, 13);
            this.Label1.TabIndex = 0;
            this.Label1.Text = "Processed:";
            //
            //Label2
            //
            this.Label2.AutoSize = true;
            this.Label2.Location = new Point(78, 50);
            this.Label2.Name = "Label2";
            this.Label2.Size = new Size(39, 13);
            this.Label2.TabIndex = 1;
            this.Label2.Text = "Label2";
            //
            //ProgressBar1
            //
            this.ProgressBar1.Location = new Point(12, 14);
            this.ProgressBar1.Name = "ProgressBar1";
            this.ProgressBar1.Size = new Size(211, 24);
            this.ProgressBar1.TabIndex = 2;
            //
            //ProgressDialog
            //
            this.AutoScaleDimensions = new SizeF(6, 13);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(235, 75);
            this.Controls.Add(this.ProgressBar1);
            this.Controls.Add(this.Label2);
            this.Controls.Add(this.Label1);
            this.Name = "ProgressDialog";
            this.Text = "Progress...";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        internal Label Label1;
        internal Label Label2;
        internal ProgressBar ProgressBar1;
    }
}