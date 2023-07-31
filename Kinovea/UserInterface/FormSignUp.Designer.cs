
namespace Kinovea.Root
{
    partial class FormSignUp
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
            this.label1 = new System.Windows.Forms.Label();
            this.txt_email = new System.Windows.Forms.TextBox();
            this.txt_name = new System.Windows.Forms.TextBox();
            this.btn_signUp = new System.Windows.Forms.Button();
            this.lbl_email = new System.Windows.Forms.Label();
            this.lbl_name = new System.Windows.Forms.Label();
            this.lbl_phoneNum = new System.Windows.Forms.Label();
            this.lbl_zip = new System.Windows.Forms.Label();
            this.txt_phoneNum = new System.Windows.Forms.TextBox();
            this.txt_zip = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Roboto", 24F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Black;
            this.label1.Location = new System.Drawing.Point(161, 34);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(225, 43);
            this.label1.TabIndex = 14;
            this.label1.Text = "ALOPEX GOLF";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // txt_email
            // 
            this.txt_email.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_email.Location = new System.Drawing.Point(176, 193);
            this.txt_email.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.txt_email.Name = "txt_email";
            this.txt_email.Size = new System.Drawing.Size(303, 26);
            this.txt_email.TabIndex = 13;
            // 
            // txt_name
            // 
            this.txt_name.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_name.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.txt_name.Location = new System.Drawing.Point(176, 124);
            this.txt_name.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.txt_name.Name = "txt_name";
            this.txt_name.Size = new System.Drawing.Size(304, 26);
            this.txt_name.TabIndex = 12;
            // 
            // btn_signUp
            // 
            this.btn_signUp.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.btn_signUp.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_signUp.Location = new System.Drawing.Point(142, 421);
            this.btn_signUp.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btn_signUp.Name = "btn_signUp";
            this.btn_signUp.Size = new System.Drawing.Size(245, 43);
            this.btn_signUp.TabIndex = 10;
            this.btn_signUp.Text = "Sign Up";
            this.btn_signUp.UseVisualStyleBackColor = false;
            this.btn_signUp.Click += new System.EventHandler(this.btn_signUp_Click);
            // 
            // lbl_email
            // 
            this.lbl_email.AutoSize = true;
            this.lbl_email.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_email.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lbl_email.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lbl_email.Location = new System.Drawing.Point(105, 196);
            this.lbl_email.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_email.Name = "lbl_email";
            this.lbl_email.Size = new System.Drawing.Size(48, 20);
            this.lbl_email.TabIndex = 9;
            this.lbl_email.Text = "Email";
            this.lbl_email.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lbl_name
            // 
            this.lbl_name.AutoSize = true;
            this.lbl_name.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_name.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lbl_name.Location = new System.Drawing.Point(102, 127);
            this.lbl_name.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_name.Name = "lbl_name";
            this.lbl_name.Size = new System.Drawing.Size(51, 20);
            this.lbl_name.TabIndex = 8;
            this.lbl_name.Text = "Name";
            this.lbl_name.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lbl_phoneNum
            // 
            this.lbl_phoneNum.AutoSize = true;
            this.lbl_phoneNum.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_phoneNum.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lbl_phoneNum.Location = new System.Drawing.Point(43, 263);
            this.lbl_phoneNum.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_phoneNum.Name = "lbl_phoneNum";
            this.lbl_phoneNum.Size = new System.Drawing.Size(115, 20);
            this.lbl_phoneNum.TabIndex = 15;
            this.lbl_phoneNum.Text = "Phone Number";
            this.lbl_phoneNum.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lbl_zip
            // 
            this.lbl_zip.AutoSize = true;
            this.lbl_zip.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_zip.ForeColor = System.Drawing.Color.DarkSlateGray;
            this.lbl_zip.Location = new System.Drawing.Point(81, 340);
            this.lbl_zip.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_zip.Name = "lbl_zip";
            this.lbl_zip.Size = new System.Drawing.Size(73, 20);
            this.lbl_zip.TabIndex = 16;
            this.lbl_zip.Text = "Zip Code";
            this.lbl_zip.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // txt_phoneNum
            // 
            this.txt_phoneNum.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_phoneNum.Location = new System.Drawing.Point(176, 261);
            this.txt_phoneNum.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.txt_phoneNum.Name = "txt_phoneNum";
            this.txt_phoneNum.Size = new System.Drawing.Size(304, 26);
            this.txt_phoneNum.TabIndex = 17;
            // 
            // txt_zip
            // 
            this.txt_zip.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_zip.Location = new System.Drawing.Point(176, 337);
            this.txt_zip.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.txt_zip.Name = "txt_zip";
            this.txt_zip.Size = new System.Drawing.Size(303, 26);
            this.txt_zip.TabIndex = 18;
            // 
            // FormSignUp
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(558, 522);
            this.Controls.Add(this.txt_zip);
            this.Controls.Add(this.txt_phoneNum);
            this.Controls.Add(this.lbl_zip);
            this.Controls.Add(this.lbl_phoneNum);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txt_email);
            this.Controls.Add(this.txt_name);
            this.Controls.Add(this.btn_signUp);
            this.Controls.Add(this.lbl_email);
            this.Controls.Add(this.lbl_name);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "FormSignUp";
            this.ShowIcon = false;
            this.Text = "Sign Up";
            this.Load += new System.EventHandler(this.FormSignUp_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txt_email;
        private System.Windows.Forms.TextBox txt_name;
        private System.Windows.Forms.Button btn_signUp;
        private System.Windows.Forms.Label lbl_email;
        private System.Windows.Forms.Label lbl_name;
        private System.Windows.Forms.Label lbl_phoneNum;
        private System.Windows.Forms.Label lbl_zip;
        private System.Windows.Forms.TextBox txt_phoneNum;
        private System.Windows.Forms.TextBox txt_zip;
    }
}