namespace KofaxIndexRecon_OnBase
{
    partial class ConfigurationForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfigurationForm));
            this.panel1 = new System.Windows.Forms.Panel();
            this.lblTop = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.tbKofaxConn = new System.Windows.Forms.TextBox();
            this.lblKofaxConn = new System.Windows.Forms.Label();
            this.tbOnBaseConn = new System.Windows.Forms.TextBox();
            this.lblOnBaseConn = new System.Windows.Forms.Label();
            this.tbLogFolder = new System.Windows.Forms.TextBox();
            this.lblLogFolder = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.tbEmailTo = new System.Windows.Forms.TextBox();
            this.lblEmailTo = new System.Windows.Forms.Label();
            this.tbEmailFrom = new System.Windows.Forms.TextBox();
            this.lblEmailFrom = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.White;
            this.panel1.Controls.Add(this.lblTop);
            this.panel1.Controls.Add(this.pictureBox1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(690, 69);
            this.panel1.TabIndex = 2;
            // 
            // lblTop
            // 
            this.lblTop.AutoSize = true;
            this.lblTop.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTop.Location = new System.Drawing.Point(69, 24);
            this.lblTop.Name = "lblTop";
            this.lblTop.Size = new System.Drawing.Size(470, 24);
            this.lblTop.TabIndex = 1;
            this.lblTop.Text = "KofaxIndexRecon_OnBase Configuration Settings";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Right;
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(598, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(92, 69);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // tbKofaxConn
            // 
            this.tbKofaxConn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbKofaxConn.Location = new System.Drawing.Point(24, 118);
            this.tbKofaxConn.Name = "tbKofaxConn";
            this.tbKofaxConn.Size = new System.Drawing.Size(578, 21);
            this.tbKofaxConn.TabIndex = 79;
            // 
            // lblKofaxConn
            // 
            this.lblKofaxConn.AutoSize = true;
            this.lblKofaxConn.Font = new System.Drawing.Font("Gadugi", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblKofaxConn.Location = new System.Drawing.Point(24, 99);
            this.lblKofaxConn.Name = "lblKofaxConn";
            this.lblKofaxConn.Size = new System.Drawing.Size(225, 19);
            this.lblKofaxConn.TabIndex = 80;
            this.lblKofaxConn.Text = "Kofax DB Connection String:";
            // 
            // tbOnBaseConn
            // 
            this.tbOnBaseConn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbOnBaseConn.Location = new System.Drawing.Point(24, 179);
            this.tbOnBaseConn.Name = "tbOnBaseConn";
            this.tbOnBaseConn.Size = new System.Drawing.Size(578, 21);
            this.tbOnBaseConn.TabIndex = 81;
            // 
            // lblOnBaseConn
            // 
            this.lblOnBaseConn.AutoSize = true;
            this.lblOnBaseConn.Font = new System.Drawing.Font("Gadugi", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblOnBaseConn.Location = new System.Drawing.Point(21, 160);
            this.lblOnBaseConn.Name = "lblOnBaseConn";
            this.lblOnBaseConn.Size = new System.Drawing.Size(293, 19);
            this.lblOnBaseConn.TabIndex = 82;
            this.lblOnBaseConn.Text = "OnBase Helper DB Connection String:";
            // 
            // tbLogFolder
            // 
            this.tbLogFolder.Location = new System.Drawing.Point(24, 294);
            this.tbLogFolder.Name = "tbLogFolder";
            this.tbLogFolder.Size = new System.Drawing.Size(581, 20);
            this.tbLogFolder.TabIndex = 83;
            // 
            // lblLogFolder
            // 
            this.lblLogFolder.AutoSize = true;
            this.lblLogFolder.Font = new System.Drawing.Font("Gadugi", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLogFolder.Location = new System.Drawing.Point(23, 272);
            this.lblLogFolder.Name = "lblLogFolder";
            this.lblLogFolder.Size = new System.Drawing.Size(163, 19);
            this.lblLogFolder.TabIndex = 84;
            this.lblLogFolder.Text = "Log Folder Location:";
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(470, 446);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(135, 26);
            this.btnCancel.TabIndex = 88;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(316, 446);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(135, 26);
            this.btnOK.TabIndex = 87;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // tbEmailTo
            // 
            this.tbEmailTo.Location = new System.Drawing.Point(131, 407);
            this.tbEmailTo.Name = "tbEmailTo";
            this.tbEmailTo.Size = new System.Drawing.Size(474, 20);
            this.tbEmailTo.TabIndex = 86;
            // 
            // lblEmailTo
            // 
            this.lblEmailTo.AutoSize = true;
            this.lblEmailTo.Font = new System.Drawing.Font("Gadugi", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblEmailTo.Location = new System.Drawing.Point(29, 406);
            this.lblEmailTo.Name = "lblEmailTo";
            this.lblEmailTo.Size = new System.Drawing.Size(79, 19);
            this.lblEmailTo.TabIndex = 90;
            this.lblEmailTo.Text = "Email To:";
            // 
            // tbEmailFrom
            // 
            this.tbEmailFrom.Location = new System.Drawing.Point(131, 365);
            this.tbEmailFrom.Name = "tbEmailFrom";
            this.tbEmailFrom.Size = new System.Drawing.Size(474, 20);
            this.tbEmailFrom.TabIndex = 85;
            // 
            // lblEmailFrom
            // 
            this.lblEmailFrom.AutoSize = true;
            this.lblEmailFrom.Font = new System.Drawing.Font("Gadugi", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblEmailFrom.Location = new System.Drawing.Point(26, 364);
            this.lblEmailFrom.Name = "lblEmailFrom";
            this.lblEmailFrom.Size = new System.Drawing.Size(99, 19);
            this.lblEmailFrom.TabIndex = 89;
            this.lblEmailFrom.Text = "Email From:";
            // 
            // ConfigurationForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(690, 520);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.tbEmailTo);
            this.Controls.Add(this.lblEmailTo);
            this.Controls.Add(this.tbEmailFrom);
            this.Controls.Add(this.lblEmailFrom);
            this.Controls.Add(this.tbLogFolder);
            this.Controls.Add(this.lblLogFolder);
            this.Controls.Add(this.tbOnBaseConn);
            this.Controls.Add(this.lblOnBaseConn);
            this.Controls.Add(this.tbKofaxConn);
            this.Controls.Add(this.lblKofaxConn);
            this.Controls.Add(this.panel1);
            this.Name = "ConfigurationForm";
            this.Text = "Settings Configuration Form";
            this.Load += new System.EventHandler(this.ConfigurationForm_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label lblTop;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TextBox tbKofaxConn;
        private System.Windows.Forms.Label lblKofaxConn;
        private System.Windows.Forms.TextBox tbOnBaseConn;
        private System.Windows.Forms.Label lblOnBaseConn;
        private System.Windows.Forms.TextBox tbLogFolder;
        private System.Windows.Forms.Label lblLogFolder;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.TextBox tbEmailTo;
        private System.Windows.Forms.Label lblEmailTo;
        private System.Windows.Forms.TextBox tbEmailFrom;
        private System.Windows.Forms.Label lblEmailFrom;
    }
}