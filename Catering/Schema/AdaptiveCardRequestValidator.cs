using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

namespace Catering.Schema
{
    public class AdaptiveCardRequestValidator
    {
        public static bool IsAdaptiveCardAction(ITurnContext turnContext)
        {
            return turnContext.Activity.Type == ActivityTypes.Invoke &&
                string.Equals(AdaptiveCardAction.Name, turnContext.Activity.Name);
        }

        public static AdaptiveCardRequest ValidateRequest(ITurnContext turnContext)
        {
            var activity = turnContext.Activity;
            AdaptiveCardRequest request = null;

            if (activity.Value == null)
            {
                AdaptiveCardActionException.BadRequest("Missing value property");
            }

            try
            {
                request = ((JObject)turnContext.Activity.Value).ToObject<AdaptiveCardRequest>();
            }
            catch(Exception)
            {
                AdaptiveCardActionException.BadRequest("Value property is not properly formed");
            }

            if (request.Action == null)
            {
                AdaptiveCardActionException.BadRequest("Missing action property");
            }

            if (request.Action.Type != AdaptiveCardsConstants.ActionExecute)
            {
                AdaptiveCardActionException.ActionNotSupported(request.Action.Type);
            }

            return request;
        }

        public static T ValidateAction<T>(AdaptiveCardRequest request)
        {
            if (request.Action.Data == null)
            {
                AdaptiveCardActionException.BadRequest("Missing data property");
            }

            try
            {
                return ((JObject)request.Action.Data).ToObject<T>();
            }
            catch(Exception)
            {
                AdaptiveCardActionException.BadRequest("Action.Data property is not properly formed");
            }

            return default(T);
        }
    }
}
