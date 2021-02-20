using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using GeoTourney;
using GeoTourney.Windows;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            var timer = new Timer
            {
                Interval = (int) TimeSpan.FromSeconds(1).TotalMilliseconds
            };
            timer.Tick += TimerOnTick;
            timer.Start();
            InitializeComponent();
            commandTextbox.KeyDown += this.OnCommandTextboxKeyDownHandler;
            commandTextbox.KeyUp += this.OnCommandTextboxKeyUpHandler;
            commandTextbox.KeyPress += this.OnCommandTextboxKeyPressHandler;
        }

        private void OnCommandTextboxKeyPressHandler(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
            }
        }

        private void OnCommandTextboxKeyUpHandler(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
            }
        }

        private void OnCommandTextboxKeyDownHandler(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                var command = commandTextbox.Text.StartsWith("!")
                    ? commandTextbox.Text.Skip(1).AsString()
                    : commandTextbox.Text;
                HandleCommand(command);
                commandTextbox.ResetText();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void TimerOnTick(object? sender, EventArgs e)
        {
            var inputCommand = ReadConsole.ReadLine(TimeSpan.FromSeconds(5));
            HandleCommand(inputCommand);
        }

        private void HandleCommand(string? inputCommand)
        {
            var commandType = inputCommand?.StartsWith("!") ?? false ? CommandType.DamnIt : CommandType.Normal;
            if (Program.page != null && Program.config != null)
            {
                var message = Task.Run(() => CommandHandler.Handle(Program.page, Program.config, inputCommand, commandType, null))
                    .GetAwaiter().GetResult();
                if (message != null) AppendLine(message);
                Application.DoEvents();
            }
        }

        private void twitchToken_TextChanged(object sender, EventArgs e) => SetUnsaved();

        private void twitchBotUsernameTextbox_TextChanged(object sender, EventArgs e) => SetUnsaved();

        private void twitchTokenLabel_Click(object sender, EventArgs e)
        {

        }

        private void twitchBotlabel_Click(object sender, EventArgs e)
        {

        }

        private void twitchChannelLabel_Click(object sender, EventArgs e)
        {

        }

        private void twitchChanneltextBox_TextChanged(object sender, EventArgs e) => SetUnsaved();

        private void saveButton_Click(object sender, EventArgs e)
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
            AppendLine("Settings saved.");
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
            AppendLine("Application started");
        }

        public void AppendLine(string text)
        {
            appOutputTextBox.AppendText($"{text}{Environment.NewLine}");
            appOutputTextBox.SelectionStart = appOutputTextBox.Text.Length;
            appOutputTextBox.ScrollToCaret();
        }

        private void githubTokenTextBox_TextChanged(object sender, EventArgs e) => SetUnsaved();

        void SetUnsaved() => saveButton.Enabled = true;
        void SetSaved() => saveButton.Enabled = false;

        private void twitchEnabledCheckBox_CheckedChanged(object sender, EventArgs e) => SetUnsaved();

        private void bigDataCloudTextBox_TextChanged(object sender, EventArgs e) => SetUnsaved();

        private void discordTokenTextBox_TextChanged(object sender, EventArgs e) => SetUnsaved();
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
