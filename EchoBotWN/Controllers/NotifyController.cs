
    using System;
    using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
    using System.Threading.Tasks;
using AdaptiveCards;
using EchoBotWN.Excel;
using EchoBotWN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Schema;
    using Microsoft.Extensions.Configuration;

namespace EchoBotWN.Controllers
{
    [Route("api/notify")]
        [ApiController]
        public class NotifyController : ControllerBase
        {
            private readonly IBotFrameworkHttpAdapter _adapter;
            private readonly string _appId;
            private readonly ConcurrentDictionary<string, ConversationReference> _conversationReferences;

            public NotifyController(IBotFrameworkHttpAdapter adapter, IConfiguration configuration, ConcurrentDictionary<string, ConversationReference> conversationReferences)
            {
                _adapter = adapter;
                _conversationReferences = conversationReferences;
                _appId = configuration["MicrosoftAppId"] ?? string.Empty;
            }

            public async Task<IActionResult> Get()
            {
                foreach (var conversationReference in _conversationReferences.Values)
                {
                    await ((BotAdapter)_adapter).ContinueConversationAsync(_appId, conversationReference, BotCallback, default(CancellationToken));
                }

                // Let the caller know proactive messages have been sent
                return new ContentResult()
                {
                    Content = "<html><body><h1>Proactive messages have been sent.</h1></body></html>",
                    ContentType = "text/html",
                    StatusCode = (int)HttpStatusCode.OK,
                };
            }

            private async Task BotCallback(ITurnContext turnContext, CancellationToken cancellationToken)
            {

            List<eventModel> events = await excel.getEvents();

            var calendarViewMessage = MessageFactory.Text("Here are your upcoming events");
            calendarViewMessage.AttachmentLayout = AttachmentLayoutTypes.List;
            var dateTimeFormat = "G";


            foreach (var calendarEvent in events)
            {

                var eventCard = CardHelper.GetEventCard(calendarEvent, dateTimeFormat);

                // Add the card to the message's attachments
                calendarViewMessage.Attachments.Add(new Microsoft.Bot.Schema.Attachment
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = eventCard
                });

            }
            await turnContext.SendActivityAsync(calendarViewMessage, cancellationToken);
            }
        }
    }