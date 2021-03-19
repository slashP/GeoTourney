using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace GeoTourney.Config
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load_1(object sender, EventArgs e)
        {
            var file = File.ReadAllText("appsettings.json");
            var appsettings = JsonSerializer.Deserialize<Appsettings>(file) ?? new Appsettings();
            twitchChanneltextBox.Text = appsettings.TwitchChannel;
            twitchBotUsernameTextbox.Text = appsettings.TwitchBotUsername;
            TwitchTokenTextBox.Text = appsettings.TwitchToken;
            bigDataCloudTextBox.Text = appsettings.BigDataCloudApiKey;
            discordTokenTextBox.Text = appsettings.DiscordToken;

            githubTokenTextBox.Text = appsettings.GithubToken;
            SetSaved();
        }

        void SetUnsaved() => saveButton.Enabled = true;

        void SetSaved() => saveButton.Enabled = false;

        private void githubTokenTextBox_TextChanged(object sender, EventArgs e) => SetUnsaved();

        private void bigDataCloudTextBox_TextChanged(object sender, EventArgs e) => SetUnsaved();

        private void discordTokenTextBox_TextChanged(object sender, EventArgs e) => SetUnsaved();

        private void TwitchTokenTextBox_TextChanged(object sender, EventArgs e) => SetUnsaved();

        private void twitchBotUsernameTextbox_TextChanged(object sender, EventArgs e) => SetUnsaved();

        private void twitchChanneltextBox_TextChanged(object sender, EventArgs e) => SetUnsaved();

        private void saveButton_Click_1(object sender, EventArgs e)
        {
            var appsettings = new Appsettings
            {
                TwitchChannel = twitchChanneltextBox.Text,
                TwitchBotUsername = twitchBotUsernameTextbox.Text,
                TwitchToken = TwitchTokenTextBox.Text,
                GithubToken = githubTokenTextBox.Text,
                BigDataCloudApiKey = bigDataCloudTextBox.Text,
                DiscordToken = discordTokenTextBox.Text
            };
            File.WriteAllText("appsettings.json", JsonSerializer.Serialize(appsettings, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
            SetSaved();
        }
    }

    internal record Appsettings
    {
        public string? TwitchChannel { get; set; }
        public string? TwitchBotUsername { get; set; }
        public string? GithubToken { get; set; }
        public string? TwitchToken { get; set; }
        public string? BigDataCloudApiKey { get; set; }
        public string? DiscordToken { get; set; }
    }
}
