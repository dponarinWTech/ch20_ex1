namespace SqlUpdater
{
    partial class MainForm
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
            this.lblDbServer = new System.Windows.Forms.Label();
            this.tbDbServer = new System.Windows.Forms.TextBox();
            this.lblDatabaseName = new System.Windows.Forms.Label();
            this.tbDatabaseName = new System.Windows.Forms.TextBox();
            this.tbConnString = new System.Windows.Forms.TextBox();
            this.lblConnString = new System.Windows.Forms.Label();
            this.lblError = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnVerify = new System.Windows.Forms.Button();
            this.lblAdminUser = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblDbServer
            // 
            this.lblDbServer.Location = new System.Drawing.Point(30, 84);
            this.lblDbServer.Name = "lblDbServer";
            this.lblDbServer.Size = new System.Drawing.Size(508, 39);
            this.lblDbServer.TabIndex = 0;
            this.lblDbServer.Text = "DB Server (SERVER\\INSTACE if multiple SQL Server instances are installed on the s" +
    "erver)";
            // 
            // tbDbServer
            // 
            this.tbDbServer.Location = new System.Drawing.Point(33, 127);
            this.tbDbServer.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tbDbServer.Name = "tbDbServer";
            this.tbDbServer.Size = new System.Drawing.Size(494, 22);
            this.tbDbServer.TabIndex = 1;
            this.tbDbServer.Text = "SADevelop";
            // 
            // lblDatabaseName
            // 
            this.lblDatabaseName.AutoSize = true;
            this.lblDatabaseName.Location = new System.Drawing.Point(30, 182);
            this.lblDatabaseName.Name = "lblDatabaseName";
            this.lblDatabaseName.Size = new System.Drawing.Size(108, 16);
            this.lblDatabaseName.TabIndex = 2;
            this.lblDatabaseName.Text = "Database Name";
            // 
            // tbDatabaseName
            // 
            this.tbDatabaseName.Location = new System.Drawing.Point(33, 202);
            this.tbDatabaseName.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tbDatabaseName.Name = "tbDatabaseName";
            this.tbDatabaseName.Size = new System.Drawing.Size(494, 22);
            this.tbDatabaseName.TabIndex = 3;
            this.tbDatabaseName.Text = "OnBase";
            // 
            // tbConnString
            // 
            this.tbConnString.Location = new System.Drawing.Point(33, 276);
            this.tbConnString.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tbConnString.Name = "tbConnString";
            this.tbConnString.ReadOnly = true;
            this.tbConnString.Size = new System.Drawing.Size(580, 22);
            this.tbConnString.TabIndex = 11;
            // 
            // lblConnString
            // 
            this.lblConnString.AutoSize = true;
            this.lblConnString.Location = new System.Drawing.Point(33, 256);
            this.lblConnString.Name = "lblConnString";
            this.lblConnString.Size = new System.Drawing.Size(112, 16);
            this.lblConnString.TabIndex = 12;
            this.lblConnString.Text = "Connection String";
            // 
            // lblError
            // 
            this.lblError.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblError.ForeColor = System.Drawing.Color.Red;
            this.lblError.Location = new System.Drawing.Point(33, 327);
            this.lblError.Name = "lblError";
            this.lblError.Size = new System.Drawing.Size(446, 58);
            this.lblError.TabIndex = 13;
            this.lblError.Text = "error";
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(38, 463);
            this.btnOK.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(134, 28);
            this.btnOK.TabIndex = 14;
            this.btnOK.Text = "Run";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(247, 463);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(134, 28);
            this.btnCancel.TabIndex = 15;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnVerify
            // 
            this.btnVerify.Location = new System.Drawing.Point(38, 410);
            this.btnVerify.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnVerify.Name = "btnVerify";
            this.btnVerify.Size = new System.Drawing.Size(134, 28);
            this.btnVerify.TabIndex = 16;
            this.btnVerify.Text = "Verify Connection";
            this.btnVerify.UseVisualStyleBackColor = true;
            this.btnVerify.Click += new System.EventHandler(this.btnVerify_Click);
            // 
            // lblAdminUser
            // 
            this.lblAdminUser.AutoSize = true;
            this.lblAdminUser.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.lblAdminUser.ForeColor = System.Drawing.Color.Maroon;
            this.lblAdminUser.Location = new System.Drawing.Point(33, 34);
            this.lblAdminUser.Name = "lblAdminUser";
            this.lblAdminUser.Size = new System.Drawing.Size(494, 16);
            this.lblAdminUser.TabIndex = 17;
            this.lblAdminUser.Text = "User running this application should have Administrator privileges on the databas" +
    "e";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(693, 520);
            this.Controls.Add(this.lblAdminUser);
            this.Controls.Add(this.btnVerify);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.lblError);
            this.Controls.Add(this.lblConnString);
            this.Controls.Add(this.tbConnString);
            this.Controls.Add(this.tbDatabaseName);
            this.Controls.Add(this.lblDatabaseName);
            this.Controls.Add(this.tbDbServer);
            this.Controls.Add(this.lblDbServer);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "MainForm";
            this.Text = "SQL Updater";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblDbServer;
        private System.Windows.Forms.TextBox tbDbServer;
        private System.Windows.Forms.Label lblDatabaseName;
        private System.Windows.Forms.TextBox tbDatabaseName;
        private System.Windows.Forms.TextBox tbConnString;
        private System.Windows.Forms.Label lblConnString;
        private System.Windows.Forms.Label lblError;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnVerify;
        private System.Windows.Forms.Label lblAdminUser;
    }
}

