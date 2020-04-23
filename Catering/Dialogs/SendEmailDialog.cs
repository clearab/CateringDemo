using Catering.Cards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Catering.Dialogs
{
    
    public class SendEmailDialog : ComponentDialog
    {
        protected readonly IConfiguration Configuration;
        protected readonly UserState UserState;
        protected readonly string _originatorId;
        
        public SendEmailDialog(UserState userState, IConfiguration configuration)
            : base(nameof(SendEmailDialog))
        {
            Configuration = configuration;
            UserState = userState;

            var connectionName = Configuration.GetSection("ConnectionName")?.Value;

            _originatorId = Configuration.GetSection("OriginatorId")?.Value;


            var steps = new WaterfallStep[] {
                SignInAsync,
                DisplayTokenAsync,
                //PromptForDifficultyAsync,
                //SaveDifficultyAsync,
                //StartWorkoutAsync,
                //EndWorkoutAsync
            };

            AddDialog(new WaterfallDialog(nameof(SendEmailDialog), steps));
            AddDialog(new OAuthPrompt(nameof(OAuthPrompt), new OAuthPromptSettings() { ConnectionName = connectionName, Text = "Please sign in to continue", Title = "Sign In" }));
        }

        private async Task<DialogTurnResult> SignInAsync(WaterfallStepContext context, CancellationToken cancellationToken)
        {
            return await context.BeginDialogAsync(
                nameof(OAuthPrompt),
                null,
                cancellationToken);
        }

        private async Task<DialogTurnResult> DisplayTokenAsync(WaterfallStepContext context, CancellationToken cancellationToken)
        {
            // Save the duration
            var tokenResponse = (TokenResponse)context.Result;

            await SendMessageAsync(context, tokenResponse.Token);

            return await context.EndDialogAsync(null, cancellationToken);
        }



        private async Task SendMessageAsync(WaterfallStepContext context, string token)
        {
            try
            {
                // Initialize Graph client with delegate auth provider
                // that just returns the token we already retrieved
                var graphClient = new GraphServiceClient(
                    new DelegateAuthenticationProvider(
                        (requestMessage) =>
                        {
                            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                            return Task.FromResult(0);
                        }));

                // Create a recipient
                var me = await graphClient.Me.Request().GetAsync();
                var toRecip = new Recipient()
                {
                    EmailAddress = new EmailAddress()
                    {
                        Address = me.Mail
                    }
                };

                // Create the message
                var actionableMessage = new Message()
                {
                    Subject = "Lunch Order Request from Catering Bot",
                    ToRecipients = new List<Recipient>() { toRecip },
                    Body = new ItemBody()
                    {
                        ContentType = BodyType.Html,
                        Content = LoadActionableMessageBody()
                    },
                    Attachments = new MessageAttachmentsCollectionPage()
                };

                // Send the message
                await graphClient.Me.SendMail(actionableMessage, true).Request().PostAsync();

                await context.Context.SendActivityAsync("Actionable message sent!");
            }
            catch (ServiceException graphEx)
            {
                await context.Context.SendActivityAsync($"Graph exception: {graphEx.Error.Code}, Message: {graphEx.Message}");
            }
            catch (Exception e)
            {
                await context.Context.SendActivityAsync($"General exception: {e.Message}");
            }
        }

        private string LoadActionableMessageBody()
        {
            // Load the card JSON
            var cardJson = JObject.Parse(new CardResource("EntreOptions.json").AsJson());

            // Check type
            // First, try "@type", which is the key MessageCard uses
            var cardType = cardJson.SelectToken("@type");
            if (cardType == null)
            {
                // Maybe it's Adaptive, try "type"
                cardType = cardJson.SelectToken("type");
            }

            // If we're still null, or the values are bad, bail
            if (cardType == null || (cardType.ToString() != "MessageCard" && cardType.ToString() != "AdaptiveCard"))
            {
                throw new ArgumentException("The payload in Card.json is missing a valid @type or type property.");
            }

            string scriptType = cardType.ToString() == "MessageCard" ? "application/ld+json" : "application/adaptivecard+json";

            // Insert originator if one is configured
            if (!string.IsNullOrEmpty(_originatorId))
            {
                // First check if there is an existing originator value
                var originator = cardJson.SelectToken("originator");

                if (originator != null)
                {
                    // Overwrite existing value
                    cardJson["originator"] = _originatorId;
                }
                else
                {
                    // Add value
                    cardJson.Add(new JProperty("originator", _originatorId));
                }
            }

            // Insert the JSON into the HTML
            return string.Format(System.IO.File.ReadAllText(@".\Resources\Message.html"), scriptType, cardJson.ToString());
        }
    }
}
