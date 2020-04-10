// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using AdaptiveCards.Templating;
using Newtonsoft.Json;

namespace Catering
{
    // This bot will respond to the user's input with an Adaptive Card.
    // Adaptive Cards are a way for developers to exchange card content
    // in a common and consistent way. A simple open card format enables
    // an ecosystem of shared tooling, seamless integration between apps,
    // and native cross-platform performance on any device.
    // For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    // This is a Transient lifetime service. Transient lifetime services are created
    // each time they're requested. For each Activity received, a new instance of this
    // class is created. Objects that are expensive to construct, or have a lifetime
    // beyond the single turn, should be carefully managed.

    public class CateringBot : ActivityHandler
    {
        private const string WelcomeText = "This bot will introduce you to AdaptiveApps. Type anything to get started.";
        private BotState _userState;

        public CateringBot(UserState userState)
        {
            _userState = userState;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            await SendWelcomeMessageAsync(turnContext, cancellationToken);
        }
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var userSA = _userState.CreateProperty<User>(nameof(User));
            var user = await userSA.GetAsync(turnContext, () => new User());

            if (turnContext.Activity.Value != null)
            {
                await processCardAction(turnContext, user, cancellationToken);
            }
            else
            {
                //user = new User();
                await sendEntreCard(turnContext, cancellationToken);
            }

        }

        private static async Task SendWelcomeMessageAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in turnContext.Activity.MembersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    var message = MessageFactory.Text($"Welcome to the Adaptive Apps Bot {member.Name}. {WelcomeText}");
                    await turnContext.SendActivityAsync(message, cancellationToken: cancellationToken);
                }
            }
        }

        private async Task processCardAction(ITurnContext<IMessageActivity> turnContext, User user, CancellationToken cancellationToken)
        {
            var data = JsonConvert.DeserializeObject<CardOptions>(turnContext.Activity.Value.ToString());

            if (data.option != null && (Card)data.currentCard == Card.Entre)
            {
                user.lunch.Entre = data.option;
            }
            else if (data.option != null && (Card)data.currentCard == Card.Drink)
            {
                user.lunch.Drink = data.option;
            }

            switch ((Card)data.nextCardToSend)
            {
                case Card.Drink:
                    await sendDrinkCard(turnContext, cancellationToken);
                    break;
                case Card.Entre:
                    await sendEntreCard(turnContext, cancellationToken);
                    break;
                case Card.Review:
                    await sendReviewCard(turnContext, user, cancellationToken);
                    break;
                case Card.ReviewAll:
                    await sendReviewAllCard(turnContext, cancellationToken);
                    break;
                case Card.Confirmation:
                    await sendConfirmationCard(turnContext, cancellationToken);
                    break;
                default:
                    throw new NotImplementedException("No card matches that nextCardToSend.");
            }


        }

        private async Task sendDrinkCard(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var filePath = Path.Combine(".", "Resources", "DrinkOptions.json");
            var card = CreateAdaptiveCardAttachment(filePath);

            await turnContext.SendActivityAsync(MessageFactory.Attachment(card), cancellationToken);
        }

        private async Task sendEntreCard(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {

            var filePath = Path.Combine(".", "Resources", "EntreOptions.json");
            var card = CreateAdaptiveCardAttachment(filePath);

            await turnContext.SendActivityAsync(MessageFactory.Attachment(card), cancellationToken);
        }

        private async Task sendReviewCard(ITurnContext<IMessageActivity> turnContext, User user, CancellationToken cancellationToken)
        {
            var filePath = Path.Combine(".", "Resources", "ReviewOrder.json");
            var lunchData = JsonConvert.SerializeObject(user.lunch);
            var adaptiveCardJson = File.ReadAllText(filePath);

            var transformer = new AdaptiveTransformer();
            var cardJson = transformer.Transform(adaptiveCardJson, lunchData);

            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(cardJson),
            };

            await turnContext.SendActivityAsync(MessageFactory.Attachment(adaptiveCardAttachment), cancellationToken);

        }

        private async Task sendReviewAllCard(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private async Task sendConfirmationCard(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var filePath = Path.Combine(".", "Resources", "Confirmation.json");
            var card = CreateAdaptiveCardAttachment(filePath);

            await turnContext.SendActivityAsync(MessageFactory.Attachment(card), cancellationToken);
        }

        private static Attachment CreateAdaptiveCardAttachment(string filePath)
        {
            var adaptiveCardJson = File.ReadAllText(filePath);
            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCardJson),
            };
            return adaptiveCardAttachment;
        }
    }

    internal class CardOptions
    {
        public int? nextCardToSend { get; set; }
        public int? currentCard { get; set; }
        public string option { get; set; }
    }

    internal class Lunch
    {
        public string Drink { get; set; }
        public string Entre { get; set; }
    }

    enum Card : int
    {
        Entre = 0,
        Drink = 1,
        Review = 2,
        ReviewAll = 3,
        Confirmation = 4
    }

    internal class User
    {
        public User()
        {
            this.lunch = new Lunch();
        }
        public string Name { get; set; }
        public Lunch lunch { get; set; }
    }
}