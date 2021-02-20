
using System.ComponentModel;

namespace WindowsFormsApp1
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.twitchChanneltextBox = new System.Windows.Forms.TextBox();
            this.twitchChannelLabel = new System.Windows.Forms.Label();
            this.twitchBotlabel = new System.Windows.Forms.Label();
            this.twitchBotUsernameTextbox = new System.Windows.Forms.TextBox();
            this.twitchTokenLabel = new System.Windows.Forms.Label();
            this.TwitchTokenTextBox = new System.Windows.Forms.TextBox();
            this.githubLabel = new System.Windows.Forms.Label();
            this.githubTokenTextBox = new System.Windows.Forms.TextBox();
            this.saveButton = new System.Windows.Forms.Button();
            this.bigDataCloudLabel = new System.Windows.Forms.Label();
            this.bigDataCloudTextBox = new System.Windows.Forms.TextBox();
            this.discordTokenLabel = new System.Windows.Forms.Label();
            this.discordTokenTextBox = new System.Windows.Forms.TextBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.settingsTabPage = new System.Windows.Forms.TabPage();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.applicationTabPage = new System.Windows.Forms.TabPage();
            this.applicationCommandLabel = new System.Windows.Forms.Label();
            this.commandTextbox = new System.Windows.Forms.TextBox();
            this.appOutputTextBox = new System.Windows.Forms.TextBox();
            this.tabControl1.SuspendLayout();
            this.settingsTabPage.SuspendLayout();
            this.applicationTabPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // twitchChanneltextBox
            // 
            this.twitchChanneltextBox.Location = new System.Drawing.Point(13, 78);
            this.twitchChanneltextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.twitchChanneltextBox.Name = "twitchChanneltextBox";
            this.twitchChanneltextBox.Size = new System.Drawing.Size(512, 23);
            this.twitchChanneltextBox.TabIndex = 0;
            this.twitchChanneltextBox.TextChanged += new System.EventHandler(this.twitchChanneltextBox_TextChanged);
            // 
            // twitchChannelLabel
            // 
            this.twitchChannelLabel.AutoSize = true;
            this.twitchChannelLabel.Location = new System.Drawing.Point(13, 60);
            this.twitchChannelLabel.Name = "twitchChannelLabel";
            this.twitchChannelLabel.Size = new System.Drawing.Size(88, 15);
            this.twitchChannelLabel.TabIndex = 1;
            this.twitchChannelLabel.Text = "Twitch Channel";
            this.twitchChannelLabel.Click += new System.EventHandler(this.twitchChannelLabel_Click);
            // 
            // twitchBotlabel
            // 
            this.twitchBotlabel.AutoSize = true;
            this.twitchBotlabel.Location = new System.Drawing.Point(13, 105);
            this.twitchBotlabel.Name = "twitchBotlabel";
            this.twitchBotlabel.Size = new System.Drawing.Size(118, 15);
            this.twitchBotlabel.TabIndex = 3;
            this.twitchBotlabel.Text = "Twitch Bot Username";
            this.twitchBotlabel.Click += new System.EventHandler(this.twitchBotlabel_Click);
            // 
            // twitchBotUsernameTextbox
            // 
            this.twitchBotUsernameTextbox.Location = new System.Drawing.Point(13, 122);
            this.twitchBotUsernameTextbox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.twitchBotUsernameTextbox.Name = "twitchBotUsernameTextbox";
            this.twitchBotUsernameTextbox.Size = new System.Drawing.Size(512, 23);
            this.twitchBotUsernameTextbox.TabIndex = 2;
            this.twitchBotUsernameTextbox.TextChanged += new System.EventHandler(this.twitchBotUsernameTextbox_TextChanged);
            // 
            // twitchTokenLabel
            // 
            this.twitchTokenLabel.AutoSize = true;
            this.twitchTokenLabel.Location = new System.Drawing.Point(13, 147);
            this.twitchTokenLabel.Name = "twitchTokenLabel";
            this.twitchTokenLabel.Size = new System.Drawing.Size(75, 15);
            this.twitchTokenLabel.TabIndex = 5;
            this.twitchTokenLabel.Text = "Twitch Token";
            this.twitchTokenLabel.Click += new System.EventHandler(this.twitchTokenLabel_Click);
            // 
            // TwitchTokenTextBox
            // 
            this.TwitchTokenTextBox.Location = new System.Drawing.Point(13, 164);
            this.TwitchTokenTextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.TwitchTokenTextBox.Name = "TwitchTokenTextBox";
            this.TwitchTokenTextBox.Size = new System.Drawing.Size(512, 23);
            this.TwitchTokenTextBox.TabIndex = 4;
            this.TwitchTokenTextBox.UseSystemPasswordChar = true;
            this.TwitchTokenTextBox.TextChanged += new System.EventHandler(this.twitchToken_TextChanged);
            // 
            // githubLabel
            // 
            this.githubLabel.AutoSize = true;
            this.githubLabel.Location = new System.Drawing.Point(13, 12);
            this.githubLabel.Name = "githubLabel";
            this.githubLabel.Size = new System.Drawing.Size(132, 15);
            this.githubLabel.TabIndex = 7;
            this.githubLabel.Text = "Github Token - required";
            // 
            // githubTokenTextBox
            // 
            this.githubTokenTextBox.Location = new System.Drawing.Point(13, 30);
            this.githubTokenTextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.githubTokenTextBox.Name = "githubTokenTextBox";
            this.githubTokenTextBox.Size = new System.Drawing.Size(512, 23);
            this.githubTokenTextBox.TabIndex = 6;
            this.githubTokenTextBox.UseSystemPasswordChar = true;
            this.githubTokenTextBox.TextChanged += new System.EventHandler(this.githubTokenTextBox_TextChanged);
            // 
            // saveButton
            // 
            this.saveButton.Location = new System.Drawing.Point(13, 364);
            this.saveButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(82, 22);
            this.saveButton.TabIndex = 9;
            this.saveButton.Text = "Save";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // bigDataCloudLabel
            // 
            this.bigDataCloudLabel.AutoSize = true;
            this.bigDataCloudLabel.Location = new System.Drawing.Point(13, 274);
            this.bigDataCloudLabel.Name = "bigDataCloudLabel";
            this.bigDataCloudLabel.Size = new System.Drawing.Size(184, 15);
            this.bigDataCloudLabel.TabIndex = 11;
            this.bigDataCloudLabel.Text = "Big Data Cloud API Key - optional";
            // 
            // bigDataCloudTextBox
            // 
            this.bigDataCloudTextBox.Location = new System.Drawing.Point(13, 292);
            this.bigDataCloudTextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.bigDataCloudTextBox.Name = "bigDataCloudTextBox";
            this.bigDataCloudTextBox.Size = new System.Drawing.Size(512, 23);
            this.bigDataCloudTextBox.TabIndex = 10;
            this.bigDataCloudTextBox.UseSystemPasswordChar = true;
            this.bigDataCloudTextBox.TextChanged += new System.EventHandler(this.bigDataCloudTextBox_TextChanged);
            // 
            // discordTokenLabel
            // 
            this.discordTokenLabel.AutoSize = true;
            this.discordTokenLabel.Location = new System.Drawing.Point(13, 322);
            this.discordTokenLabel.Name = "discordTokenLabel";
            this.discordTokenLabel.Size = new System.Drawing.Size(136, 15);
            this.discordTokenLabel.TabIndex = 12;
            this.discordTokenLabel.Text = "Discord Token - optional";
            // 
            // discordTokenTextBox
            // 
            this.discordTokenTextBox.Location = new System.Drawing.Point(13, 339);
            this.discordTokenTextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.discordTokenTextBox.Name = "discordTokenTextBox";
            this.discordTokenTextBox.Size = new System.Drawing.Size(512, 23);
            this.discordTokenTextBox.TabIndex = 13;
            this.discordTokenTextBox.UseSystemPasswordChar = true;
            this.discordTokenTextBox.TextChanged += new System.EventHandler(this.discordTokenTextBox_TextChanged);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.settingsTabPage);
            this.tabControl1.Controls.Add(this.applicationTabPage);
            this.tabControl1.Location = new System.Drawing.Point(10, 9);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1097, 433);
            this.tabControl1.TabIndex = 14;
            // 
            // settingsTabPage
            // 
            this.settingsTabPage.Controls.Add(this.label2);
            this.settingsTabPage.Controls.Add(this.label1);
            this.settingsTabPage.Controls.Add(this.textBox1);
            this.settingsTabPage.Controls.Add(this.discordTokenTextBox);
            this.settingsTabPage.Controls.Add(this.twitchChanneltextBox);
            this.settingsTabPage.Controls.Add(this.discordTokenLabel);
            this.settingsTabPage.Controls.Add(this.twitchChannelLabel);
            this.settingsTabPage.Controls.Add(this.bigDataCloudLabel);
            this.settingsTabPage.Controls.Add(this.twitchBotUsernameTextbox);
            this.settingsTabPage.Controls.Add(this.bigDataCloudTextBox);
            this.settingsTabPage.Controls.Add(this.twitchBotlabel);
            this.settingsTabPage.Controls.Add(this.saveButton);
            this.settingsTabPage.Controls.Add(this.TwitchTokenTextBox);
            this.settingsTabPage.Controls.Add(this.twitchTokenLabel);
            this.settingsTabPage.Controls.Add(this.githubLabel);
            this.settingsTabPage.Controls.Add(this.githubTokenTextBox);
            this.settingsTabPage.Location = new System.Drawing.Point(4, 24);
            this.settingsTabPage.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.settingsTabPage.Name = "settingsTabPage";
            this.settingsTabPage.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.settingsTabPage.Size = new System.Drawing.Size(1089, 405);
            this.settingsTabPage.TabIndex = 0;
            this.settingsTabPage.Text = "Settings";
            this.settingsTabPage.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(102, 368);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(368, 15);
            this.label2.TabIndex = 17;
            this.label2.Text = "After saving, close and restart the application before they take effect.";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 192);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(197, 15);
            this.label1.TabIndex = 16;
            this.label1.Text = "Create Twitch token by copying URL";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(13, 209);
            this.textBox1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(512, 23);
            this.textBox1.TabIndex = 15;
            this.textBox1.Text = "https://id.twitch.tv/oauth2/authorize?response_type=token&client_id=u3gs2ijo0y4uy" +
    "pz2vjbj4r49vapx0e&redirect_uri=https://twitchapps.com/tokengen/&scope=chat%3Area" +
    "d%20chat%3Aedit";
            // 
            // applicationTabPage
            // 
            this.applicationTabPage.Controls.Add(this.applicationCommandLabel);
            this.applicationTabPage.Controls.Add(this.commandTextbox);
            this.applicationTabPage.Controls.Add(this.appOutputTextBox);
            this.applicationTabPage.Location = new System.Drawing.Point(4, 24);
            this.applicationTabPage.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.applicationTabPage.Name = "applicationTabPage";
            this.applicationTabPage.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.applicationTabPage.Size = new System.Drawing.Size(1089, 405);
            this.applicationTabPage.TabIndex = 1;
            this.applicationTabPage.Text = "Application";
            this.applicationTabPage.UseVisualStyleBackColor = true;
            // 
            // applicationCommandLabel
            // 
            this.applicationCommandLabel.AutoSize = true;
            this.applicationCommandLabel.Location = new System.Drawing.Point(4, 342);
            this.applicationCommandLabel.Name = "applicationCommandLabel";
            this.applicationCommandLabel.Size = new System.Drawing.Size(118, 15);
            this.applicationCommandLabel.TabIndex = 2;
            this.applicationCommandLabel.Text = "Type command here:";
            // 
            // commandTextbox
            // 
            this.commandTextbox.Location = new System.Drawing.Point(4, 360);
            this.commandTextbox.Name = "commandTextbox";
            this.commandTextbox.Size = new System.Drawing.Size(660, 23);
            this.commandTextbox.TabIndex = 1;
            // 
            // appOutputTextBox
            // 
            this.appOutputTextBox.Location = new System.Drawing.Point(-1, 0);
            this.appOutputTextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.appOutputTextBox.Multiline = true;
            this.appOutputTextBox.Name = "appOutputTextBox";
            this.appOutputTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.appOutputTextBox.Size = new System.Drawing.Size(1092, 327);
            this.appOutputTextBox.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1141, 445);
            this.Controls.Add(this.tabControl1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "Form1";
            this.Text = "GeoTourney";
            this.Load += new System.EventHandler(this.Form1_Load_1);
            this.tabControl1.ResumeLayout(false);
            this.settingsTabPage.ResumeLayout(false);
            this.settingsTabPage.PerformLayout();
            this.applicationTabPage.ResumeLayout(false);
            this.applicationTabPage.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox twitchChanneltextBox;
        private System.Windows.Forms.Label twitchChannelLabel;
        private System.Windows.Forms.Label twitchBotlabel;
        private System.Windows.Forms.TextBox githubTokenTextBox;
        private System.Windows.Forms.TextBox twitchBotUsernameTextbox;
        private System.Windows.Forms.Label twitchTokenLabel;
        private System.Windows.Forms.TextBox TwitchTokenTextBox;
        private System.Windows.Forms.Label githubLabel;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.Label bigDataCloudLabel;
        private System.Windows.Forms.TextBox bigDataCloudTextBox;
        private System.Windows.Forms.Label discordTokenLabel;
        private System.Windows.Forms.TextBox discordTokenTextBox;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage settingsTabPage;
        private System.Windows.Forms.TabPage applicationTabPage;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox1;
        public System.Windows.Forms.TextBox appOutputTextBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label applicationCommandLabel;
        private System.Windows.Forms.TextBox commandTextbox;
    }
}

