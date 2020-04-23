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
using Catering.Cards;
using Catering.Models;
using System.Net;
using AdaptiveCards;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.Bot.AdaptiveCards;
using System.Linq;
using Microsoft.AspNetCore.Http.Features;

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
        private const string WelcomeText = "Welcome to the Adaptive Cards 2.0 Bot. This bot will introduce you to Action.Execute in Adaptive Cards. Type anything to get started.";
        private BotState _userState;
        private CateringDb _cateringDb;
        private readonly CateringRecognizer _cateringRecognizer;

        public CateringBot(UserState userState, CateringDb cateringDb, CateringRecognizer cateringRecognizer)
        {
            _userState = userState;
            _cateringDb = cateringDb;
            _cateringRecognizer = cateringRecognizer;
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

            var text = turnContext.Activity.Text.ToLowerInvariant();

            if (text.Contains("recent"))
            {
                await SendRecentOrdersCardMessage(turnContext, cancellationToken);
            }
            else
            {
                if (turnContext.Activity.Value != null)
                {
                    await processCardAction(turnContext, user, cancellationToken);
                }
                else
                {
                    await SendEntreCardMessage(turnContext, cancellationToken);
                }
            }
        }

        protected override async Task<InvokeResponse> OnInvokeActivityAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            if (AdaptiveCardInvokeValidator.IsAdaptiveCardAction(turnContext))
            {
                var userSA = _userState.CreateProperty<User>(nameof(User));
                var user = await userSA.GetAsync(turnContext, () => new User() { Id = turnContext.Activity.From.Id });

                try
                {
                    AdaptiveCardInvoke request = AdaptiveCardInvokeValidator.ValidateRequest(turnContext);

                    if (request.Action.Verb == "order")
                    {
                        var cardOptions = AdaptiveCardInvokeValidator.ValidateAction<CardOptions>(request);

                        // process action
                        var responseBody = await ProcessOrderAction(user, cardOptions);
                        
                        return CreateInvokeResponse(HttpStatusCode.OK, responseBody);
                    }
                    else
                    {
                        AdaptiveCardActionException.VerbNotSupported(request.Action.Type);
                    }
                }
                catch (AdaptiveCardActionException e)
                {
                    return CreateInvokeResponse(HttpStatusCode.OK, e.Response);
                }
            }

            return null;
        }

        private async Task<AdaptiveCardInvokeResponse> ProcessOrderAction(User user, CardOptions cardOptions)
        {
            if ((Card)cardOptions.currentCard == Card.Entre)
            {
                if (!string.IsNullOrEmpty(cardOptions.custom))
                {
                    if (!await _cateringRecognizer.ValidateEntre(cardOptions.custom))
                    {
                        return RedoEntreCardResponse(new Lunch() { Entre = cardOptions.custom });
                    }
                    cardOptions.option = cardOptions.custom;
                }

                user.Lunch.Entre = cardOptions.option;
            }
            else if ((Card)cardOptions.currentCard == Card.Drink)
            {
                if (!string.IsNullOrEmpty(cardOptions.custom))
                {
                    if (!await _cateringRecognizer.ValidateDrink(cardOptions.custom))
                    {
                        return RedoDrinkCardResponse(new Lunch() { Drink = cardOptions.custom });
                    }

                    cardOptions.option = cardOptions.custom;
                }

                user.Lunch.Drink = cardOptions.option;
            }

            AdaptiveCardInvokeResponse responseBody = null;
            switch ((Card)cardOptions.nextCardToSend)
            {
                case Card.Drink:
                    responseBody = DrinkCardResponse();
                    break;
                case Card.Entre:
                    responseBody = EntreCardResponse();
                    break;
                case Card.Review:
                    responseBody = ReviewCardResponse(user);
                    break;
                case Card.ReviewAll:
                    var latestOrders = await _cateringDb.GetRecentOrdersAsync();
                    responseBody = RecentOrdersCardResponse(latestOrders.Items);
                    break;
                case Card.Confirmation:
                    await _cateringDb.AddOrderAsync(user);
                    responseBody = ConfirmationCardResponse();
                    break;
                default:
                    throw new NotImplementedException("No card matches that nextCardToSend.");
            }

            return responseBody;
        }

        private static InvokeResponse CreateInvokeResponse(HttpStatusCode statusCode, object body = null)
        {
            return new InvokeResponse()
            {
                Status = (int)statusCode,
                Body = body
            };
        }

        private static async Task SendWelcomeMessageAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in turnContext.Activity.MembersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    var message = MessageFactory.Text(WelcomeText);
                    await turnContext.SendActivityAsync(message, cancellationToken: cancellationToken);
                }
            }
        }

        private async Task processCardAction(ITurnContext<IMessageActivity> turnContext, User user, CancellationToken cancellationToken)
        {
            var data = JsonConvert.DeserializeObject<CardOptions>(turnContext.Activity.Value.ToString());

            if (data.option != null && (Card)data.currentCard == Card.Entre)
            {
                user.Lunch.Entre = data.option;
            }
            else if (data.option != null && (Card)data.currentCard == Card.Drink)
            {
                user.Lunch.Drink = data.option;
            }

            switch ((Card)data.nextCardToSend)
            {
                case Card.Drink:
                    await SendDrinkCardMessage(turnContext, cancellationToken);
                    break;
                case Card.Entre:
                    await SendEntreCardMessage(turnContext, cancellationToken);
                    break;
                case Card.Review:
                    await SendReviewCardMessage(turnContext, user, cancellationToken);
                    break;
                case Card.ReviewAll:
                    await SendRecentOrdersCardMessage(turnContext, cancellationToken);
                    break;
                case Card.Confirmation:
                    await SendConfirmationCardMessage(turnContext, cancellationToken);
                    break;
                default:
                    throw new NotImplementedException("No card matches that nextCardToSend.");
            }
        }

        #region Cards As MessageActivities

        private async Task SendDrinkCardMessage(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(
                MessageFactory.Attachment(new CardResource("DrinkOptions.json").AsAttachment()), cancellationToken);
        }

        private async Task SendEntreCardMessage(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(
                MessageFactory.Attachment(new CardResource("EntreOptions.json").AsAttachment()), cancellationToken);
        }

        private async Task SendReviewCardMessage(ITurnContext turnContext, User user, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(
                MessageFactory.Attachment(new CardResource("ReviewOrder.json").AsAttachment(user.Lunch)), cancellationToken);
        }

        private async Task SendConfirmationCardMessage(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(
                MessageFactory.Attachment(new CardResource("Confirmation.json").AsAttachment()), cancellationToken);
        }

        private async Task SendRecentOrdersCardMessage(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var latestOrders = await _cateringDb.GetRecentOrdersAsync();
            var users = latestOrders.Items;
            await turnContext.SendActivityAsync(
                MessageFactory.Attachment(new CardResource("RecentOrders.json").AsAttachment(
                    new
                    {
                        users = users.Select(u => new
                        {
                            lunch = new
                            {
                                entre = u.Lunch.Entre,
                                drink = u.Lunch.Drink,
                                orderTimestamp = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(u.Lunch.OrderTimestamp, "Pacific Standard Time").ToString("g")
                            }
                        }).ToList()
                    })), cancellationToken);
        }

        #endregion

        #region Cards As InvokeResponses

        private AdaptiveCardInvokeResponse CardResponse(string cardFileName)
        {
            return new AdaptiveCardInvokeResponse()
            {
                StatusCode = 200,
                Type = AdaptiveCard.ContentType,
                Value = new CardResource(cardFileName).AsJObject()
            };
        }

        private AdaptiveCardInvokeResponse CardResponse<T>(string cardFileName, T data)
        {
            return new AdaptiveCardInvokeResponse()
            {
                StatusCode = 200,
                Type = AdaptiveCard.ContentType,
                Value = new CardResource(cardFileName).AsJObject(data)
            };
        }

        private AdaptiveCardInvokeResponse DrinkCardResponse()
        {
            return CardResponse("DrinkOptions.json");
        }

        private AdaptiveCardInvokeResponse EntreCardResponse()
        {
            return CardResponse("EntreOptions.json");
        }

        private AdaptiveCardInvokeResponse ReviewCardResponse(User user)
        {
            return CardResponse("ReviewOrder.json", user.Lunch);
        }

        private AdaptiveCardInvokeResponse RedoDrinkCardResponse(Lunch lunch)
        {
            return CardResponse("RedoDrinkOptions.json", lunch);
        }

        private AdaptiveCardInvokeResponse RedoEntreCardResponse(Lunch lunch)
        {
            return CardResponse("RedoEntreOptions.json", lunch);
        }

        private AdaptiveCardInvokeResponse RecentOrdersCardResponse(IList<User> users)
        {
            return CardResponse("RecentOrders.json", 
                new
                {
                    users = users.Select(u => new 
                    { 
                        lunch = new
                        { 
                            entre = u.Lunch.Entre,
                            drink = u.Lunch.Drink,
                            orderTimestamp = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(u.Lunch.OrderTimestamp, "Pacific Standard Time").ToString("g")
                        }
                    }).ToList()
                });
        }

        private AdaptiveCardInvokeResponse ConfirmationCardResponse()
        {
            return CardResponse("Confirmation.json");
        }

        #endregion
    }

    internal class CardOptions
    {
        public int? nextCardToSend { get; set; }
        public int? currentCard { get; set; }
        public string option { get; set; }
        public string custom { get; set; }
    }

   

    enum Card : int
    {
        Entre = 0,
        Drink = 1,
        Review = 2,
        ReviewAll = 3,
        Confirmation = 4
    }

    
}