# ReviewBot

ReviewBot is a Reddit Bot that uses the Whisky Review Archive from
[/r/scotch](https://www.reddit.com/r/Scotch/) to automatically list your latest
reviews.

## Running
The bot can run locally or in Azure. In order to list the reviews rapidly, the
bot keeps a local cache of all reviews in a NoSQL database. In Azure it uses
CosmosDB. When run locally, it uses LiteDB as a file-based NoSQL database.

To run the bot in Azure, deploy the JanssenIo.ReviewBot.Azure project to
an Azure Function.

To run the bot locally, start the JanssenIo.ReviewBot.ArchiveParser project
to download the local cache of the whisky archive. Then run the 
JanssenIo.ReviewBot.Replies project to have the bot scan its inbox for mentions.

Whether the bot runs locally or in Azure, it only checks its inbox once. So it
should be run on a timer trigger. The Azure Function uses a 5-minute timer
trigger to do this. Locally, you could use cron.

## Configuration
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
| ReviewBot:AppSecret  | The Client Secret of the Bot in Reddit | Required for authentication with Reddit |
| ReviewBot:RefreshToken  | The Refresh Token from the OAuth2 flow | Required by the .NET Library to authenticate with Reddit |
| Store:ConnectionString  | The credentials to connect with CosmosDB | Required to read and write the local review cache |

## Deployment
The Azure infrastructure can be deployed using the Bicep file in the root of the
repository. In the .github folder there is a workflow that creates a release and
deploy the infrastructure. The Azure Function then pulls the latest code from
the GitHub release.

The workflow uses federated credentials so that we don't need to manage expiring
secrets ourselves.