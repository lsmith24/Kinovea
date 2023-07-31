
namespace Kinovea.Root
{
    partial class FormExportVideos
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
            this.lbl_user = new System.Windows.Forms.Label();
            this.lbl_date = new System.Windows.Forms.Label();
            this.lbl_userName = new System.Windows.Forms.Label();
            this.lbl_date2 = new System.Windows.Forms.Label();
            this.lbl_selectVid = new System.Windows.Forms.Label();
            this.lbl_instructor = new System.Windows.Forms.Label();
            this.list_instructors = new System.Windows.Forms.ListBox();
            this.lbl_instructorEmail = new System.Windows.Forms.Label();
            this.lbl_userEmail = new System.Windows.Forms.Label();
            this.btn_sendVideo = new System.Windows.Forms.Button();
            this.btn_cancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lbl_user
            // 
            this.lbl_user.AutoSize = true;
            this.lbl_user.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_user.Location = new System.Drawing.Point(41, 156);
            this.lbl_user.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_user.Name = "lbl_user";
            this.lbl_user.Size = new System.Drawing.Size(53, 26);
            this.lbl_user.TabIndex = 0;
            this.lbl_user.Text = "User";
            // 
            // lbl_date
            // 
            this.lbl_date.AutoSize = true;
            this.lbl_date.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_date.Location = new System.Drawing.Point(40, 50);
            this.lbl_date.Name = "lbl_date";
            this.lbl_date.Size = new System.Drawing.Size(54, 26);
            this.lbl_date.TabIndex = 1;
            this.lbl_date.Text = "Date";
            // 
            // lbl_userName
            // 
            this.lbl_userName.AutoSize = true;
            this.lbl_userName.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_userName.Location = new System.Drawing.Point(90, 158);
            this.lbl_userName.Name = "lbl_userName";
            this.lbl_userName.Size = new System.Drawing.Size(0, 24);
            this.lbl_userName.TabIndex = 2;
            // 
            // lbl_date2
            // 
            this.lbl_date2.AutoSize = true;
            this.lbl_date2.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_date2.Location = new System.Drawing.Point(89, 52);
            this.lbl_date2.Name = "lbl_date2";
            this.lbl_date2.Size = new System.Drawing.Size(0, 24);
            this.lbl_date2.TabIndex = 3;
            // 
            // lbl_selectVid
            // 
            this.lbl_selectVid.AutoSize = true;
            this.lbl_selectVid.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_selectVid.Location = new System.Drawing.Point(42, 435);
            this.lbl_selectVid.Name = "lbl_selectVid";
            this.lbl_selectVid.Size = new System.Drawing.Size(134, 26);
            this.lbl_selectVid.TabIndex = 4;
            this.lbl_selectVid.Text = "Select Videos";
            // 
            // lbl_instructor
            // 
            this.lbl_instructor.AutoSize = true;
            this.lbl_instructor.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_instructor.Location = new System.Drawing.Point(42, 290);
            this.lbl_instructor.Name = "lbl_instructor";
            this.lbl_instructor.Size = new System.Drawing.Size(99, 26);
            this.lbl_instructor.TabIndex = 5;
            this.lbl_instructor.Text = "Instructor";
            // 
            // list_instructors
            // 
            this.list_instructors.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.list_instructors.FormattingEnabled = true;
            this.list_instructors.ItemHeight = 25;
            this.list_instructors.Location = new System.Drawing.Point(46, 323);
            this.list_instructors.Name = "list_instructors";
            this.list_instructors.Size = new System.Drawing.Size(248, 54);
            this.list_instructors.TabIndex = 6;
            this.list_instructors.SelectedIndexChanged += new System.EventHandler(this.list_instructors_SelectedIndexChanged);
            // 
            // lbl_instructorEmail
            // 
            this.lbl_instructorEmail.AutoSize = true;
            this.lbl_instructorEmail.Font = new System.Drawing.Font("Roboto", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_instructorEmail.Location = new System.Drawing.Point(43, 390);
            this.lbl_instructorEmail.Name = "lbl_instructorEmail";
            this.lbl_instructorEmail.Size = new System.Drawing.Size(0, 20);
            this.lbl_instructorEmail.TabIndex = 7;
            // 
            // lbl_userEmail
            // 
            this.lbl_userEmail.AutoSize = true;
            this.lbl_userEmail.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_userEmail.Location = new System.Drawing.Point(42, 192);
            this.lbl_userEmail.Name = "lbl_userEmail";
            this.lbl_userEmail.Size = new System.Drawing.Size(0, 24);
            this.lbl_userEmail.TabIndex = 8;
            // 
            // btn_sendVideo
            // 
            this.btn_sendVideo.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.btn_sendVideo.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_sendVideo.Location = new System.Drawing.Point(118, 729);
            this.btn_sendVideo.Name = "btn_sendVideo";
            this.btn_sendVideo.Size = new System.Drawing.Size(237, 44);
            this.btn_sendVideo.TabIndex = 9;
            this.btn_sendVideo.Text = "Send";
            this.btn_sendVideo.UseVisualStyleBackColor = false;
            // 
            // btn_cancel
            // 
            this.btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btn_cancel.Font = new System.Drawing.Font("Roboto", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_cancel.Location = new System.Drawing.Point(389, 734);
            this.btn_cancel.Name = "btn_cancel";
            this.btn_cancel.Size = new System.Drawing.Size(135, 39);
            this.btn_cancel.TabIndex = 10;
            this.btn_cancel.Text = "Cancel";
            this.btn_cancel.UseVisualStyleBackColor = true;
            // 
            // FormExportVideos
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.AutoSize = true;
            this.CancelButton = this.btn_cancel;
            this.ClientSize = new System.Drawing.Size(689, 811);
            this.Controls.Add(this.btn_cancel);
            this.Controls.Add(this.btn_sendVideo);
            this.Controls.Add(this.lbl_userEmail);
            this.Controls.Add(this.lbl_instructorEmail);
            this.Controls.Add(this.list_instructors);
            this.Controls.Add(this.lbl_instructor);
            this.Controls.Add(this.lbl_selectVid);
            this.Controls.Add(this.lbl_date2);
            this.Controls.Add(this.lbl_userName);
            this.Controls.Add(this.lbl_date);
            this.Controls.Add(this.lbl_user);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "FormExportVideos";
            this.ShowIcon = false;
            this.Text = "Export Videos";
            this.Load += new System.EventHandler(this.FormExportVideos_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbl_user;
        private System.Windows.Forms.Label lbl_date;
        private System.Windows.Forms.Label lbl_userName;
        private System.Windows.Forms.Label lbl_date2;
        private System.Windows.Forms.Label lbl_selectVid;
        private System.Windows.Forms.Label lbl_instructor;
        private System.Windows.Forms.ListBox list_instructors;
        private System.Windows.Forms.Label lbl_instructorEmail;
        private System.Windows.Forms.Label lbl_userEmail;
        private System.Windows.Forms.Button btn_sendVideo;
        private System.Windows.Forms.Button btn_cancel;
    }
}