### `appsettings.json`

#### Provide 4 values in the appsettings file
```
{
  "GithubToken": "GITHUB_TOKEN",
  "TwitchChannel": "Slashpeek",
  "TwitchBotUsername": "slashpeekbot",
  "TwitchToken": "TWITCH_TOKEN"
}
```

#### GITHUB_TOKEN

1. Create a Github account. You can use an existing, but I can't recommend that. The program will put files into a `geoguessr` folder in the [Github Pages](https://pages.github.com/) repository.
2. Go to https://github.com/settings/tokens and click `Generate new token`.
3. Check the scope `public_repo`.
4. Put `Geoguessr Twitch tournament` or similar in the `Note` field so you know what it's for.
5. Generate token.
6. Copy the token value and put it into your `appsettings.json` file.

#### TWITCH_TOKEN

1. Log into the Twitch account you would like to send messages about tournament results to your chat. This is the same account you should put into `TwitchBotUsername` in the appsettings file.
2. Go to https://twitchapps.com/tokengen/
3. Put `u3gs2ijo0y4uypz2vjbj4r49vapx0e` into the `Client ID` field.
4. Put `chat:read chat:edit` into the Scopes field.
5. Copy the token value and put it into your `appsettings.json` file.

### How to use
1. Download and unzip the .exe to a folder of your choice.
2. Run `Geoguessr.exe` from command line.
3. This opens a browser. Log into the geoguessr account you will be playing with. NB: After logging in, minimize this browser window, you should not use this as the browser you play in.
4. Send a challenge link to the Twitch chat you're hosting, f.ex. `https://www.geoguessr.com/challenge/kdrp4V1ByTC2D7Qr` Only the link, nothing else.
5. The bot responds with `Game #1: https://www.geoguessr.com/challenge/kdrp4V1ByTC2D7Qr`.
6. Play the challenge after your audience has had some time. When finished the bot will post a link to results page, f.ex. https://slashpeekbot.github.io/geoguessr/v3/tournament.html?id=637449452465947316
7. Use `!restart` in Twitch chat to forget current tournament and start over. Use `!totalscore` to get a results page with all games and points summed. These commands only work for the streamer/broadcaster.