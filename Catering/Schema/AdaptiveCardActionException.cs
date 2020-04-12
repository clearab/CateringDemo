using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Catering.Schema
{
    public class AdaptiveCardActionException : Exception
    {
        AdaptiveCardActionException(AdaptiveCardResponse response)
        {
            Response = response;
        }

        public AdaptiveCardResponse Response { get; private set; }

        public static void ErrorResponse(HttpStatusCode statusCode, string code, string message)
        {
            throw new AdaptiveCardActionException(new AdaptiveCardResponse()
            {
                StatusCode = (int)statusCode,
                Name = AdaptiveCardsConstants.Error,
                Value = new Error()
                {
                    Code = code,
                    Message = message
                }
            });
        }

        public static void ActionNotSupported(string action)
        {
            ErrorResponse(HttpStatusCode.BadRequest, "NotSupported", $"The action '{action}'is not supported.");
        }

        public static void VerbNotSupported(string verb)
        {
            ErrorResponse(HttpStatusCode.BadRequest, "NotSupported", $"The verb '{verb}'is not supported.");
        }

        public static void BadRequest(string message)
        {
            ErrorResponse(HttpStatusCode.BadRequest, "BadRequest", message);
        }
    }
}
