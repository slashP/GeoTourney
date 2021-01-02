### First time setup
To run the application you need `GeoTourney.exe` and a valid `appsettings.json` file. The `appsettings.json` file is a text file that looks like this:

##### `appsettings.json`
```
{
  "TwitchChannel": "TWITCH_CHANNEL",
  "TwitchBotUsername": "TWITCH_BOT",
  "TwitchToken": "TWITCH_TOKEN",
  "GithubToken": "GITHUB_TOKEN"
}
```

Download and unzip the `GeoTourney.exe` and `appsettings.json` to a folder of your choice. Open that file in a text editor - now you will replace each of the upper case items with proper values.

###### TWITCH_CHANNEL
This is the streamer's channel/username on Twitch. Example `slashpeek`. The application listens to messages from this channel when it runs.

###### TWITCH_BOT
This is the Twitch account the application will send chat messages on behalf of. *Either* create a separate bot account or use your streamer account. Mark that the application is quite "conversational" so it most likely feels more natural to use a separate bot account so that it doesn't look like you're talking to yourself. Example: `slashpeekbot`.

###### TWITCH_TOKEN

1. Log into the Twitch account you decided to use for `TWITCH_BOT` above.
2. Go to https://twitchapps.com/tokengen/
3. Put `u3gs2ijo0y4uypz2vjbj4r49vapx0e` into the `Client ID` field.
4. Put `chat:read chat:edit` into the Scopes field.
5. Copy the token value and put it into your `appsettings.json` file.

![Create the Twitch access token](setup/twitch_access_token.png "Twitch access token")

###### GITHUB_TOKEN

1. Create a Github account. You can use an existing, but I can't recommend that. The program will put files into a `geoguessr` folder in the [Github Pages](https://pages.github.com/) repository.
2. After registering, completing the hilariously hard reCAPTCHA and signing in, go to https://github.com/settings/tokens and click `Generate new token`.
3. Check the scope `public_repo`.
4. Put `Geoguessr Twitch tournament` or similar in the `Note` field so you know what it's for.
5. Generate token.
6. Copy the token value and put it into your `appsettings.json` file.

![Create the Github access token](setup/github_access_token.png "Github access token")

### How to use
1. Run `Geoguessr.exe` by double clicking (or from command line). The first time this will take time as it downloads ~300 MB.
2. Then a browser opens. Log into the geoguessr account you will be playing with. NB: After logging in, minimize this browser window, you **cannot** use this as the browser you play in.
3. Send challenge links to the Twitch chat you're hosting, f.ex. `https://www.geoguessr.com/challenge/kdrp4V1ByTC2D7Qr` Only the link, nothing else.
4. The bot responds with `Game #1: https://www.geoguessr.com/challenge/kdrp4V1ByTC2D7Qr`.
5. Play the challenge after your audience has had some time. When finished the bot will post a link to results page, f.ex. https://slashpeekbot.github.io/geoguessr/v4.7/tournament.html?id=637451944357099056
6. Commands. These commands only work for the streamer/broadcaster.
  * `!totalscore` to get a results page with all games and points summed.
  * `!restart` to forget current tournament and start over.
  * `!elim` to toggle elimination mode on/off.

  ![Gameplay in action](setup/game_in_action.png "Gameplay in action")