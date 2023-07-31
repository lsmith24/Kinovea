
namespace Kinovea.Root
{
    partial class FormStartSession
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
            this.lbl_numUsers = new System.Windows.Forms.Label();
            this.list_numUsers = new System.Windows.Forms.ListBox();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lbl_numUsers
            // 
            this.lbl_numUsers.AutoSize = true;
            this.lbl_numUsers.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_numUsers.Location = new System.Drawing.Point(28, 56);
            this.lbl_numUsers.Name = "lbl_numUsers";
            this.lbl_numUsers.Size = new System.Drawing.Size(222, 24);
            this.lbl_numUsers.TabIndex = 0;
            this.lbl_numUsers.Text = "Select Number of Users";
            // 
            // list_numUsers
            // 
            this.list_numUsers.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.list_numUsers.FormattingEnabled = true;
            this.list_numUsers.ItemHeight = 24;
            this.list_numUsers.Items.AddRange(new object[] {
            "1",
            "2"});
            this.list_numUsers.Location = new System.Drawing.Point(32, 110);
            this.list_numUsers.Name = "list_numUsers";
            this.list_numUsers.Size = new System.Drawing.Size(301, 52);
            this.list_numUsers.TabIndex = 1;
            this.list_numUsers.SelectedIndexChanged += new System.EventHandler(this.list_numUsers_SelectedIndexChanged);
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.button1.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.Location = new System.Drawing.Point(165, 211);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(205, 67);
            this.button1.TabIndex = 2;
            this.button1.Text = "Continue";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // FormStartSession
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(542, 354);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.list_numUsers);
            this.Controls.Add(this.lbl_numUsers);
            this.Name = "FormStartSession";
            this.ShowIcon = false;
            this.Text = "Start New Alopex Session";
            this.Load += new System.EventHandler(this.FormStartSession_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbl_numUsers;
        private System.Windows.Forms.ListBox list_numUsers;
        private System.Windows.Forms.Button button1;
    }
}