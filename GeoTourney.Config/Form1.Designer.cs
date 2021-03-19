
namespace GeoTourney.Config
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
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.discordTokenTextBox = new System.Windows.Forms.TextBox();
            this.twitchChanneltextBox = new System.Windows.Forms.TextBox();
            this.discordTokenLabel = new System.Windows.Forms.Label();
            this.twitchChannelLabel = new System.Windows.Forms.Label();
            this.bigDataCloudLabel = new System.Windows.Forms.Label();
            this.twitchBotUsernameTextbox = new System.Windows.Forms.TextBox();
            this.bigDataCloudTextBox = new System.Windows.Forms.TextBox();
            this.twitchBotlabel = new System.Windows.Forms.Label();
            this.saveButton = new System.Windows.Forms.Button();
            this.TwitchTokenTextBox = new System.Windows.Forms.TextBox();
            this.twitchTokenLabel = new System.Windows.Forms.Label();
            this.githubLabel = new System.Windows.Forms.Label();
            this.githubTokenTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(101, 352);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(368, 15);
            this.label2.TabIndex = 33;
            this.label2.Text = "After saving, close and restart the application before they take effect.";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 193);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(197, 15);
            this.label1.TabIndex = 32;
            this.label1.Text = "Create Twitch token by copying URL";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(12, 210);
            this.textBox1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(512, 23);
            this.textBox1.TabIndex = 31;
            this.textBox1.Text = "https://id.twitch.tv/oauth2/authorize?response_type=token&client_id=u3gs2ijo0y4uy" +
    "pz2vjbj4r49vapx0e&redirect_uri=https://twitchapps.com/tokengen/&scope=chat%3Area" +
    "d%20chat%3Aedit";
            // 
            // discordTokenTextBox
            // 
            this.discordTokenTextBox.Location = new System.Drawing.Point(12, 310);
            this.discordTokenTextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.discordTokenTextBox.Name = "discordTokenTextBox";
            this.discordTokenTextBox.Size = new System.Drawing.Size(512, 23);
            this.discordTokenTextBox.TabIndex = 30;
            this.discordTokenTextBox.UseSystemPasswordChar = true;
            this.discordTokenTextBox.TextChanged += new System.EventHandler(this.discordTokenTextBox_TextChanged);
            // 
            // twitchChanneltextBox
            // 
            this.twitchChanneltextBox.Location = new System.Drawing.Point(12, 79);
            this.twitchChanneltextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.twitchChanneltextBox.Name = "twitchChanneltextBox";
            this.twitchChanneltextBox.Size = new System.Drawing.Size(512, 23);
            this.twitchChanneltextBox.TabIndex = 18;
            this.twitchChanneltextBox.TextChanged += new System.EventHandler(this.twitchChanneltextBox_TextChanged);
            // 
            // discordTokenLabel
            // 
            this.discordTokenLabel.AutoSize = true;
            this.discordTokenLabel.Location = new System.Drawing.Point(12, 293);
            this.discordTokenLabel.Name = "discordTokenLabel";
            this.discordTokenLabel.Size = new System.Drawing.Size(136, 15);
            this.discordTokenLabel.TabIndex = 29;
            this.discordTokenLabel.Text = "Discord Token - optional";
            // 
            // twitchChannelLabel
            // 
            this.twitchChannelLabel.AutoSize = true;
            this.twitchChannelLabel.Location = new System.Drawing.Point(12, 61);
            this.twitchChannelLabel.Name = "twitchChannelLabel";
            this.twitchChannelLabel.Size = new System.Drawing.Size(88, 15);
            this.twitchChannelLabel.TabIndex = 19;
            this.twitchChannelLabel.Text = "Twitch Channel";
            // 
            // bigDataCloudLabel
            // 
            this.bigDataCloudLabel.AutoSize = true;
            this.bigDataCloudLabel.Location = new System.Drawing.Point(12, 245);
            this.bigDataCloudLabel.Name = "bigDataCloudLabel";
            this.bigDataCloudLabel.Size = new System.Drawing.Size(184, 15);
            this.bigDataCloudLabel.TabIndex = 28;
            this.bigDataCloudLabel.Text = "Big Data Cloud API Key - optional";
            // 
            // twitchBotUsernameTextbox
            // 
            this.twitchBotUsernameTextbox.Location = new System.Drawing.Point(12, 123);
            this.twitchBotUsernameTextbox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.twitchBotUsernameTextbox.Name = "twitchBotUsernameTextbox";
            this.twitchBotUsernameTextbox.Size = new System.Drawing.Size(512, 23);
            this.twitchBotUsernameTextbox.TabIndex = 20;
            this.twitchBotUsernameTextbox.TextChanged += new System.EventHandler(this.twitchBotUsernameTextbox_TextChanged);
            // 
            // bigDataCloudTextBox
            // 
            this.bigDataCloudTextBox.Location = new System.Drawing.Point(12, 263);
            this.bigDataCloudTextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.bigDataCloudTextBox.Name = "bigDataCloudTextBox";
            this.bigDataCloudTextBox.Size = new System.Drawing.Size(512, 23);
            this.bigDataCloudTextBox.TabIndex = 27;
            this.bigDataCloudTextBox.UseSystemPasswordChar = true;
            this.bigDataCloudTextBox.TextChanged += new System.EventHandler(this.bigDataCloudTextBox_TextChanged);
            // 
            // twitchBotlabel
            // 
            this.twitchBotlabel.AutoSize = true;
            this.twitchBotlabel.Location = new System.Drawing.Point(12, 106);
            this.twitchBotlabel.Name = "twitchBotlabel";
            this.twitchBotlabel.Size = new System.Drawing.Size(118, 15);
            this.twitchBotlabel.TabIndex = 21;
            this.twitchBotlabel.Text = "Twitch Bot Username";
            // 
            // saveButton
            // 
            this.saveButton.Location = new System.Drawing.Point(12, 348);
            this.saveButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(82, 22);
            this.saveButton.TabIndex = 26;
            this.saveButton.Text = "Save";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click_1);
            // 
            // TwitchTokenTextBox
            // 
            this.TwitchTokenTextBox.Location = new System.Drawing.Point(12, 165);
            this.TwitchTokenTextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.TwitchTokenTextBox.Name = "TwitchTokenTextBox";
            this.TwitchTokenTextBox.Size = new System.Drawing.Size(512, 23);
            this.TwitchTokenTextBox.TabIndex = 22;
            this.TwitchTokenTextBox.UseSystemPasswordChar = true;
            this.TwitchTokenTextBox.TextChanged += new System.EventHandler(this.TwitchTokenTextBox_TextChanged);
            // 
            // twitchTokenLabel
            // 
            this.twitchTokenLabel.AutoSize = true;
            this.twitchTokenLabel.Location = new System.Drawing.Point(12, 148);
            this.twitchTokenLabel.Name = "twitchTokenLabel";
            this.twitchTokenLabel.Size = new System.Drawing.Size(75, 15);
            this.twitchTokenLabel.TabIndex = 23;
            this.twitchTokenLabel.Text = "Twitch Token";
            // 
            // githubLabel
            // 
            this.githubLabel.AutoSize = true;
            this.githubLabel.Location = new System.Drawing.Point(12, 13);
            this.githubLabel.Name = "githubLabel";
            this.githubLabel.Size = new System.Drawing.Size(132, 15);
            this.githubLabel.TabIndex = 25;
            this.githubLabel.Text = "Github Token - required";
            // 
            // githubTokenTextBox
            // 
            this.githubTokenTextBox.Location = new System.Drawing.Point(12, 31);
            this.githubTokenTextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.githubTokenTextBox.Name = "githubTokenTextBox";
            this.githubTokenTextBox.Size = new System.Drawing.Size(512, 23);
            this.githubTokenTextBox.TabIndex = 24;
            this.githubTokenTextBox.UseSystemPasswordChar = true;
            this.githubTokenTextBox.TextChanged += new System.EventHandler(this.githubTokenTextBox_TextChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(564, 390);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.discordTokenTextBox);
            this.Controls.Add(this.twitchChanneltextBox);
            this.Controls.Add(this.discordTokenLabel);
            this.Controls.Add(this.twitchChannelLabel);
            this.Controls.Add(this.bigDataCloudLabel);
            this.Controls.Add(this.twitchBotUsernameTextbox);
            this.Controls.Add(this.bigDataCloudTextBox);
            this.Controls.Add(this.twitchBotlabel);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.TwitchTokenTextBox);
            this.Controls.Add(this.twitchTokenLabel);
            this.Controls.Add(this.githubLabel);
            this.Controls.Add(this.githubTokenTextBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "Form1";
            this.Text = "GeoTourney";
            this.Load += new System.EventHandler(this.Form1_Load_1);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.TextBox discordTokenTextBox;
        private System.Windows.Forms.TextBox twitchChanneltextBox;
        private System.Windows.Forms.Label discordTokenLabel;
        private System.Windows.Forms.Label twitchChannelLabel;
        private System.Windows.Forms.Label bigDataCloudLabel;
        private System.Windows.Forms.TextBox twitchBotUsernameTextbox;
        private System.Windows.Forms.TextBox bigDataCloudTextBox;
        private System.Windows.Forms.Label twitchBotlabel;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.TextBox TwitchTokenTextBox;
        private System.Windows.Forms.Label twitchTokenLabel;
        private System.Windows.Forms.Label githubLabel;
        private System.Windows.Forms.TextBox githubTokenTextBox;
    }
}

