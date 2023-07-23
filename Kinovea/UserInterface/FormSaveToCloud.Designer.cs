
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
            this.btnUploadToCloud = new System.Windows.Forms.Button();
            this.btnCancelUpload = new System.Windows.Forms.Button();
            this.cmbox_InstructorList = new System.Windows.Forms.ComboBox();
            this.lbl_instructor = new System.Windows.Forms.Label();
            this.txtbx_Notes = new System.Windows.Forms.TextBox();
            this.lbl_Notes = new System.Windows.Forms.Label();
            this.lbl_videoSelect = new System.Windows.Forms.Label();
            this.lbl_user = new System.Windows.Forms.Label();
            this.lbl_date = new System.Windows.Forms.Label();
            this.lbl_userName = new System.Windows.Forms.Label();
            this.lbl_dataFill = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnUploadToCloud
            // 
            this.btnUploadToCloud.BackColor = System.Drawing.Color.DarkSlateGray;
            this.btnUploadToCloud.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUploadToCloud.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnUploadToCloud.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.btnUploadToCloud.Location = new System.Drawing.Point(133, 473);
            this.btnUploadToCloud.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnUploadToCloud.Name = "btnUploadToCloud";
            this.btnUploadToCloud.Size = new System.Drawing.Size(131, 41);
            this.btnUploadToCloud.TabIndex = 0;
            this.btnUploadToCloud.Text = "Upload";
            this.btnUploadToCloud.UseVisualStyleBackColor = false;
            this.btnUploadToCloud.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnCancelUpload
            // 
            this.btnCancelUpload.Font = new System.Drawing.Font("Roboto", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCancelUpload.Location = new System.Drawing.Point(303, 473);
            this.btnCancelUpload.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnCancelUpload.Name = "btnCancelUpload";
            this.btnCancelUpload.Size = new System.Drawing.Size(92, 41);
            this.btnCancelUpload.TabIndex = 1;
            this.btnCancelUpload.Text = "Cancel";
            this.btnCancelUpload.UseVisualStyleBackColor = true;
            // 
            // cmbox_InstructorList
            // 
            this.cmbox_InstructorList.FormattingEnabled = true;
            this.cmbox_InstructorList.Items.AddRange(new object[] {
            "Lindsay Smith",
            "Ryan Hosler",
            "Instructor Abigail"});
            this.cmbox_InstructorList.Location = new System.Drawing.Point(133, 265);
            this.cmbox_InstructorList.Name = "cmbox_InstructorList";
            this.cmbox_InstructorList.Size = new System.Drawing.Size(302, 25);
            this.cmbox_InstructorList.TabIndex = 2;
            // 
            // lbl_instructor
            // 
            this.lbl_instructor.AutoSize = true;
            this.lbl_instructor.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_instructor.Location = new System.Drawing.Point(36, 271);
            this.lbl_instructor.Name = "lbl_instructor";
            this.lbl_instructor.Size = new System.Drawing.Size(76, 19);
            this.lbl_instructor.TabIndex = 3;
            this.lbl_instructor.Text = "Instructor";
            this.lbl_instructor.Click += new System.EventHandler(this.label1_Click);
            // 
            // txtbx_Notes
            // 
            this.txtbx_Notes.Font = new System.Drawing.Font("Roboto", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtbx_Notes.ForeColor = System.Drawing.SystemColors.InactiveCaption;
            this.txtbx_Notes.Location = new System.Drawing.Point(133, 323);
            this.txtbx_Notes.Multiline = true;
            this.txtbx_Notes.Name = "txtbx_Notes";
            this.txtbx_Notes.Size = new System.Drawing.Size(302, 115);
            this.txtbx_Notes.TabIndex = 4;
            this.txtbx_Notes.Text = "Club used, What was worked on, Reason for sending, etc.";
            // 
            // lbl_Notes
            // 
            this.lbl_Notes.AutoSize = true;
            this.lbl_Notes.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_Notes.Location = new System.Drawing.Point(62, 325);
            this.lbl_Notes.Name = "lbl_Notes";
            this.lbl_Notes.Size = new System.Drawing.Size(50, 19);
            this.lbl_Notes.TabIndex = 5;
            this.lbl_Notes.Text = "Notes";
            this.lbl_Notes.Click += new System.EventHandler(this.label2_Click);
            // 
            // lbl_videoSelect
            // 
            this.lbl_videoSelect.AutoSize = true;
            this.lbl_videoSelect.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_videoSelect.Location = new System.Drawing.Point(36, 66);
            this.lbl_videoSelect.Name = "lbl_videoSelect";
            this.lbl_videoSelect.Size = new System.Drawing.Size(177, 19);
            this.lbl_videoSelect.TabIndex = 6;
            this.lbl_videoSelect.Text = "Select Videos for Export";
            // 
            // lbl_user
            // 
            this.lbl_user.AutoSize = true;
            this.lbl_user.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_user.Location = new System.Drawing.Point(36, 25);
            this.lbl_user.Name = "lbl_user";
            this.lbl_user.Size = new System.Drawing.Size(44, 19);
            this.lbl_user.TabIndex = 7;
            this.lbl_user.Text = "User";
            this.lbl_user.Click += new System.EventHandler(this.label1_Click_1);
            // 
            // lbl_date
            // 
            this.lbl_date.AutoSize = true;
            this.lbl_date.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_date.Location = new System.Drawing.Point(258, 25);
            this.lbl_date.Name = "lbl_date";
            this.lbl_date.Size = new System.Drawing.Size(46, 19);
            this.lbl_date.TabIndex = 8;
            this.lbl_date.Text = "Date";
            this.lbl_date.Click += new System.EventHandler(this.label1_Click_2);
            // 
            // lbl_userName
            // 
            this.lbl_userName.AutoSize = true;
            this.lbl_userName.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_userName.Location = new System.Drawing.Point(86, 25);
            this.lbl_userName.Name = "lbl_userName";
            this.lbl_userName.Size = new System.Drawing.Size(0, 19);
            this.lbl_userName.TabIndex = 9;
            // 
            // lbl_dataFill
            // 
            this.lbl_dataFill.AutoSize = true;
            this.lbl_dataFill.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_dataFill.Location = new System.Drawing.Point(310, 25);
            this.lbl_dataFill.Name = "lbl_dataFill";
            this.lbl_dataFill.Size = new System.Drawing.Size(0, 19);
            this.lbl_dataFill.TabIndex = 10;
            // 
            // FormSaveToCloud
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ClientSize = new System.Drawing.Size(487, 536);
            this.Controls.Add(this.lbl_dataFill);
            this.Controls.Add(this.lbl_userName);
            this.Controls.Add(this.lbl_date);
            this.Controls.Add(this.lbl_user);
            this.Controls.Add(this.lbl_videoSelect);
            this.Controls.Add(this.lbl_Notes);
            this.Controls.Add(this.txtbx_Notes);
            this.Controls.Add(this.lbl_instructor);
            this.Controls.Add(this.cmbox_InstructorList);
            this.Controls.Add(this.btnCancelUpload);
            this.Controls.Add(this.btnUploadToCloud);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "FormSaveToCloud";
            this.Text = "Alopex Golf";
            this.Load += new System.EventHandler(this.FormSaveToCloud_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnUploadToCloud;
        private System.Windows.Forms.Button btnCancelUpload;
        private System.Windows.Forms.ComboBox cmbox_InstructorList;
        private System.Windows.Forms.Label lbl_instructor;
        private System.Windows.Forms.TextBox txtbx_Notes;
        private System.Windows.Forms.Label lbl_Notes;
        private System.Windows.Forms.Label lbl_videoSelect;
        private System.Windows.Forms.Label lbl_user;
        private System.Windows.Forms.Label lbl_date;
        private System.Windows.Forms.Label lbl_userName;
        private System.Windows.Forms.Label lbl_dataFill;
    }
}