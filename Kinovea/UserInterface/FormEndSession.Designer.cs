
namespace Kinovea.Root
{
    partial class FormEndSession
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
            this.lbl_sureend = new System.Windows.Forms.Label();
            this.lbl_message = new System.Windows.Forms.Label();
            this.btn_confirm = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lbl_sureend
            // 
            this.lbl_sureend.AutoSize = true;
            this.lbl_sureend.Font = new System.Drawing.Font("Roboto", 16.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_sureend.Location = new System.Drawing.Point(68, 47);
            this.lbl_sureend.Name = "lbl_sureend";
            this.lbl_sureend.Size = new System.Drawing.Size(532, 34);
            this.lbl_sureend.TabIndex = 0;
            this.lbl_sureend.Text = "Are you sure you want to end the session?";
            // 
            // lbl_message
            // 
            this.lbl_message.AutoSize = true;
            this.lbl_message.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_message.Location = new System.Drawing.Point(70, 137);
            this.lbl_message.Name = "lbl_message";
            this.lbl_message.Size = new System.Drawing.Size(540, 48);
            this.lbl_message.TabIndex = 1;
            this.lbl_message.Text = "Current users will be logged out. \r\nRemember to export your videos before ending " +
    "the session!";
            this.lbl_message.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // btn_confirm
            // 
            this.btn_confirm.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.btn_confirm.Font = new System.Drawing.Font("Roboto", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_confirm.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btn_confirm.Location = new System.Drawing.Point(160, 250);
            this.btn_confirm.Name = "btn_confirm";
            this.btn_confirm.Size = new System.Drawing.Size(372, 56);
            this.btn_confirm.TabIndex = 2;
            this.btn_confirm.Text = "End Session";
            this.btn_confirm.UseVisualStyleBackColor = false;
            this.btn_confirm.Click += new System.EventHandler(this.btn_confirm_Click);
            // 
            // button1
            // 
            this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button1.Font = new System.Drawing.Font("Roboto", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.Location = new System.Drawing.Point(254, 340);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(190, 50);
            this.button1.TabIndex = 3;
            this.button1.Text = "cancel";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // FormEndSession
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.AutoSize = true;
            this.CancelButton = this.button1;
            this.ClientSize = new System.Drawing.Size(666, 443);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btn_confirm);
            this.Controls.Add(this.lbl_message);
            this.Controls.Add(this.lbl_sureend);
            this.Name = "FormEndSession";
            this.ShowIcon = false;
            this.Text = "End Session";
            this.Load += new System.EventHandler(this.FormEndSession_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbl_sureend;
        private System.Windows.Forms.Label lbl_message;
        private System.Windows.Forms.Button btn_confirm;
        private System.Windows.Forms.Button button1;
    }
}