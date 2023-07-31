
namespace Kinovea.Root
{
    partial class FormSaveToCloud
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
            this.btnUploadToCloud = new System.Windows.Forms.Button();
            this.btnCancelUpload = new System.Windows.Forms.Button();
            this.lbl_instructor = new System.Windows.Forms.Label();
            this.txtbx_Notes = new System.Windows.Forms.TextBox();
            this.lbl_Notes = new System.Windows.Forms.Label();
            this.lbl_user = new System.Windows.Forms.Label();
            this.lbl_date = new System.Windows.Forms.Label();
            this.lbl_userName = new System.Windows.Forms.Label();
            this.lbl_dataFill = new System.Windows.Forms.Label();
            this.txt_Club = new System.Windows.Forms.TextBox();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.txt_swingName = new System.Windows.Forms.TextBox();
            this.list_users = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // btnUploadToCloud
            // 
            this.btnUploadToCloud.BackColor = System.Drawing.Color.DarkSlateGray;
            this.btnUploadToCloud.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUploadToCloud.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnUploadToCloud.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.btnUploadToCloud.Location = new System.Drawing.Point(159, 736);
            this.btnUploadToCloud.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnUploadToCloud.Name = "btnUploadToCloud";
            this.btnUploadToCloud.Size = new System.Drawing.Size(131, 41);
            this.btnUploadToCloud.TabIndex = 0;
            this.btnUploadToCloud.Text = "Save";
            this.btnUploadToCloud.UseVisualStyleBackColor = false;
            this.btnUploadToCloud.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnCancelUpload
            // 
            this.btnCancelUpload.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancelUpload.Font = new System.Drawing.Font("Roboto", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCancelUpload.Location = new System.Drawing.Point(329, 736);
            this.btnCancelUpload.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnCancelUpload.Name = "btnCancelUpload";
            this.btnCancelUpload.Size = new System.Drawing.Size(92, 41);
            this.btnCancelUpload.TabIndex = 1;
            this.btnCancelUpload.Text = "Cancel";
            this.btnCancelUpload.UseVisualStyleBackColor = true;
            this.btnCancelUpload.Click += new System.EventHandler(this.btnCancelUpload_Click);
            // 
            // lbl_instructor
            // 
            this.lbl_instructor.AutoSize = true;
            this.lbl_instructor.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_instructor.Location = new System.Drawing.Point(59, 399);
            this.lbl_instructor.Name = "lbl_instructor";
            this.lbl_instructor.Size = new System.Drawing.Size(52, 26);
            this.lbl_instructor.TabIndex = 3;
            this.lbl_instructor.Text = "Club";
            this.lbl_instructor.Click += new System.EventHandler(this.label1_Click);
            // 
            // txtbx_Notes
            // 
            this.txtbx_Notes.Font = new System.Drawing.Font("Roboto", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtbx_Notes.ForeColor = System.Drawing.SystemColors.ControlText;
            this.txtbx_Notes.Location = new System.Drawing.Point(62, 564);
            this.txtbx_Notes.Multiline = true;
            this.txtbx_Notes.Name = "txtbx_Notes";
            this.txtbx_Notes.Size = new System.Drawing.Size(467, 116);
            this.txtbx_Notes.TabIndex = 4;
            // 
            // lbl_Notes
            // 
            this.lbl_Notes.AutoSize = true;
            this.lbl_Notes.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_Notes.Location = new System.Drawing.Point(59, 518);
            this.lbl_Notes.Name = "lbl_Notes";
            this.lbl_Notes.Size = new System.Drawing.Size(65, 26);
            this.lbl_Notes.TabIndex = 5;
            this.lbl_Notes.Text = "Notes";
            this.lbl_Notes.Click += new System.EventHandler(this.label2_Click);
            // 
            // lbl_user
            // 
            this.lbl_user.AutoSize = true;
            this.lbl_user.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_user.Location = new System.Drawing.Point(57, 124);
            this.lbl_user.Name = "lbl_user";
            this.lbl_user.Size = new System.Drawing.Size(114, 26);
            this.lbl_user.TabIndex = 7;
            this.lbl_user.Text = "Select User";
            this.lbl_user.Click += new System.EventHandler(this.label1_Click_1);
            // 
            // lbl_date
            // 
            this.lbl_date.AutoSize = true;
            this.lbl_date.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_date.Location = new System.Drawing.Point(57, 49);
            this.lbl_date.Name = "lbl_date";
            this.lbl_date.Size = new System.Drawing.Size(54, 26);
            this.lbl_date.TabIndex = 8;
            this.lbl_date.Text = "Date";
            this.lbl_date.Click += new System.EventHandler(this.label1_Click_2);
            // 
            // lbl_userName
            // 
            this.lbl_userName.AutoSize = true;
            this.lbl_userName.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_userName.Location = new System.Drawing.Point(117, 51);
            this.lbl_userName.Name = "lbl_userName";
            this.lbl_userName.Size = new System.Drawing.Size(0, 24);
            this.lbl_userName.TabIndex = 9;
            // 
            // lbl_dataFill
            // 
            this.lbl_dataFill.AutoSize = true;
            this.lbl_dataFill.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_dataFill.Location = new System.Drawing.Point(106, 51);
            this.lbl_dataFill.Name = "lbl_dataFill";
            this.lbl_dataFill.Size = new System.Drawing.Size(0, 24);
            this.lbl_dataFill.TabIndex = 10;
            // 
            // txt_Club
            // 
            this.txt_Club.Font = new System.Drawing.Font("Roboto", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_Club.Location = new System.Drawing.Point(63, 444);
            this.txt_Club.Name = "txt_Club";
            this.txt_Club.Size = new System.Drawing.Size(466, 30);
            this.txt_Club.TabIndex = 11;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(59, 280);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(124, 26);
            this.label1.TabIndex = 13;
            this.label1.Text = "Swing Name";
            // 
            // txt_swingName
            // 
            this.txt_swingName.Font = new System.Drawing.Font("Roboto", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_swingName.Location = new System.Drawing.Point(62, 326);
            this.txt_swingName.Name = "txt_swingName";
            this.txt_swingName.Size = new System.Drawing.Size(467, 30);
            this.txt_swingName.TabIndex = 14;
            // 
            // list_users
            // 
            this.list_users.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.list_users.FormattingEnabled = true;
            this.list_users.ItemHeight = 24;
            this.list_users.Location = new System.Drawing.Point(62, 160);
            this.list_users.Name = "list_users";
            this.list_users.Size = new System.Drawing.Size(376, 76);
            this.list_users.TabIndex = 15;
            this.list_users.SelectedIndexChanged += new System.EventHandler(this.list_users_SelectedIndexChanged);
            // 
            // FormSaveToCloud
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.AutoSize = true;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.CancelButton = this.btnCancelUpload;
            this.ClientSize = new System.Drawing.Size(638, 822);
            this.Controls.Add(this.list_users);
            this.Controls.Add(this.txt_swingName);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txt_Club);
            this.Controls.Add(this.lbl_dataFill);
            this.Controls.Add(this.lbl_userName);
            this.Controls.Add(this.lbl_date);
            this.Controls.Add(this.lbl_user);
            this.Controls.Add(this.lbl_Notes);
            this.Controls.Add(this.txtbx_Notes);
            this.Controls.Add(this.lbl_instructor);
            this.Controls.Add(this.btnCancelUpload);
            this.Controls.Add(this.btnUploadToCloud);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "FormSaveToCloud";
            this.ShowIcon = false;
            this.Text = "Alopex Golf";
            this.Load += new System.EventHandler(this.FormSaveToCloud_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnUploadToCloud;
        private System.Windows.Forms.Button btnCancelUpload;
        private System.Windows.Forms.Label lbl_instructor;
        private System.Windows.Forms.TextBox txtbx_Notes;
        private System.Windows.Forms.Label lbl_Notes;
        private System.Windows.Forms.Label lbl_user;
        private System.Windows.Forms.Label lbl_date;
        private System.Windows.Forms.Label lbl_userName;
        private System.Windows.Forms.Label lbl_dataFill;
        private System.Windows.Forms.TextBox txt_Club;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txt_swingName;
        private System.Windows.Forms.ListBox list_users;
    }
}