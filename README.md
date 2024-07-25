# ReviewBot

ReviewBot is a Reddit Bot that uses the Whisky Review Archive from
[/r/scotch](https://www.reddit.com/r/Scotch/) to automatically list your latest
reviews. It can be summoned in [/r/scotch](https://reddit.com/r/scotch), [/r/bourbon](https://reddit.com/r/bourbon), [/r/worldwhisky](https://reddit.com/r/worldwhisky).

## Summoning
If any of the following commands appear in the bot's inbox, he will reply accordingly.

**List latest reviews**

List up to 10 of the users latest reviews in any subreddit.

    /u/review_bot latest

**List latest reviews in \<subreddit\>**

List up to 10 of the users latest reviews in the given subreddit. (Substitute \<subreddit\> with the subreddit name, e.g. /r/scotch)

    /u/review_bot /r/<subreddit>

**List latest reviews about \<bottle\>**

List up to 10 of the users latest reviews about a certain bottle. This command searches for a substring, so asking for Talisker reviews might return reviews about Talisker Storm, Talisker 10yr and Talisker DE.
As with the subreddit command, substitute \<bottle\> with the name of the bottle, but be sure to use single quotes or double quotes.

    /u/review_bot '<bottle>'

**List latest reviews about \<region\>**

List up to 10 of the users latest reviews about bottles from a specific region. This command searches for a substring, so asking for "land" reviews will return reviews about bottles from both the Highlands and the Lowlands (and "Islands" if you didn't specify that further).

As with the subreddit command, substitute \<region\> with the name of the bottle, but be sure to use single quotes or double quotes.

    /u/review_bot '<region>'

## Running / Installation
Below are the technical details for if you want to run this bot yourself.

The bot can run locally or in Microsoft Azure. In order to list the reviews rapidly, the
bot keeps a local cache of all reviews in a NoSQL database. In Azure it uses
CosmosDB. When run locally, it uses LiteDB as a file-based NoSQL database.

To run the bot in Azure, deploy the JanssenIo.ReviewBot.Functions project to
an Azure Function.

To run the bot locally, start the JanssenIo.ReviewBot.ArchiveParser project
to download the local cache of the whisky archive. Then run the 
JanssenIo.ReviewBot.Replies project to have the bot scan its inbox for mentions.

Whether the bot runs locally or in Azure, it only checks its inbox once. So it
should be run on a timer trigger. The Azure Function uses a 5-minute timer
trigger to do this. Locally, you could use cron.

### Configuration
The bot requires a secrets.json file to run locally. 
To create this file, run the `JanssenIo.ReviewBot.Replies/CreateSecrets.cs`
script with the following environmnet variables set.

| Key | Description | Notes |
|-----|-------------|--------
| AI_IKEY | Applicaton Insights Instrumentation Key | Required for logging and metrics |
| BOT_ID  | The Client ID of the Bot in Reddit | Required for authentication with Reddit |
| BOT_SECRET  | The Client Secret of the Bot in Reddit | Required for authentication with Reddit |
| BOT_REFRESHTOKEN  | The Refresh Token from the OAuth2 flow | Required by the .NET Library to authenticate with Reddit |
| LITEDB_CONNECTIONSTRING  | The credentials to connect with LiteDB | Required to read and write the local review cache |

When these variables are set, the secrets.json can be 

For Azure, the following secrets must exist in the key vault:

| Key | Description | Notes |
|-----|-------------|--------
| AppSecret  | The Client Secret of the Bot in Reddit | Required for authentication with Reddit |
| RefreshToken  | The Refresh Token from the OAuth2 flow | Required by the .NET Library to authenticate with Reddit |
| ConnectionString  | The credentials to connect with CosmosDB | Required to read and write the local review cache |

### Deployment
The Azure infrastructure can be deployed using the Bicep file in the root of the
repository. In the .github folder there is a workflow that creates a release and
deploy the infrastructure. The Azure Function then pulls the latest code from
the GitHub release.

The workflow uses federated credentials so that we don't need to manage expiring
secrets ourselves.
