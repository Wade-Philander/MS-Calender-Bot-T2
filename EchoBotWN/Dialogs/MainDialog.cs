using AdaptiveCards;
using EchoBotWN.Excel;
using EchoBotWN.Graph;
using EchoBotWN.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EchoBotWN.Dialogs
{
    public class MainDialog : LogoutDialog
    {
        const string NO_PROMPT = "no-prompt";
        protected readonly ILogger _logger;
        public MainDialog(IConfiguration configuration,
            ILogger<MainDialog> logger)
            : base(nameof(MainDialog), configuration["ConnectionName"])
        {
            _logger = logger;

          /*  AddDialog(new OAuthPrompt(
                nameof(OAuthPrompt),
                new OAuthPromptSettings
                {
                    ConnectionName = ConnectionName,
                    Text = "Please login",
                    Title = "Login",
                    Timeout = 300000,
                })); */
           
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));

            AddDialog(new NewEventDialog(configuration));

            AddDialog(new DeleteEventDialog(configuration));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                PromptUserStepAsync,
                CommandStepAsync,
                ReturnToPromptStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }


        private async Task<DialogTurnResult> PromptUserStepAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text("Please choose an option below"),
                Choices = new List<Choice> {
                    new Choice { Value = "Show calendar" },
                    new Choice { Value = "Add event" },
                    new Choice { Value = "Delete event" },
                }
            };

            return await stepContext.PromptAsync(
                nameof(ChoicePrompt),
                options,
                cancellationToken);
        }

        private async Task<DialogTurnResult> CommandStepAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            // Save the command the user entered so we can get it back after
            // the OAuthPrompt completes
            var foundChoice = stepContext.Result as FoundChoice;
            // Result could be a FoundChoice (if user selected a choice button)
            // or a string (if user just typed something)
            stepContext.Values["command"] = foundChoice?.Value ?? stepContext.Result;

            if (stepContext.Result != null)
            {
                var command = ((string)stepContext.Values["command"] ?? string.Empty).ToLowerInvariant();

                if (command.StartsWith("show calendar"))
                {
                    await DisplayCalendarView(stepContext, cancellationToken);
                }
                else if (command.StartsWith("add event"))
                {
                    return await stepContext.BeginDialogAsync(nameof(NewEventDialog), null, cancellationToken);
                }
                else if (command.StartsWith("delete event"))
                {
                    return await stepContext.BeginDialogAsync(nameof(DeleteEventDialog), null, cancellationToken);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(
                        MessageFactory.Text("I'm sorry, I didn't understand. Please try again."),
                        cancellationToken);
                }
            }


            // Go to the next step
            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }

 
        private async Task DisplayCalendarView(
    WaterfallStepContext stepContext,
    CancellationToken cancellationToken)
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

            await stepContext.Context.SendActivityAsync(calendarViewMessage, cancellationToken);
        }


        private async Task<DialogTurnResult> ReturnToPromptStepAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            // Restart the dialog, but skip the initial login prompt
            return await stepContext.ReplaceDialogAsync(InitialDialogId, NO_PROMPT, cancellationToken);
        }

        [FunctionName("TimerTriggerCSharp")]
        public static void Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer, ILogger log)
        {
            if (myTimer.IsPastDue)
            {
                log.LogInformation("Timer is running late!");
            }
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }

        public async void ah()
        {
            //Teams channel id in which to create the post.
            string teamsChannelId = "29:12Umzo4fYYEEtMe2rlzKgxOeDYRFKEsqO-cilpTFjVOK6Ntdod72xqkeCzz5zXyopHOyC6ct_DdwMLOMvcZgIgg";

            //The Bot Service Url needs to be dynamically fetched (and stored) from the Team. Recommendation is to capture the serviceUrl from the bot Payload and later re-use it to send proactive messages.
            string serviceUrl = "https://smba.trafficmanager.net/za/";

            //From the Bot Channel Registration
            string botClientID = "75cda236-8d0f-4b47-ad01-d3395eaf22a1";
            string botClientSecret = "EZ88Q~qHWLLJEZ7oFOyt9aM7okfc5nG1vGeF0cL3";

            var account = new MicrosoftAppCredentials(botClientID, botClientSecret);
            var jwtToken = await account.GetTokenAsync();
            ConnectorClient connector = new ConnectorClient(new System.Uri(serviceUrl), account);


            MicrosoftAppCredentials.TrustServiceUrl(serviceUrl);
            var connectorClient = new ConnectorClient(new Uri(serviceUrl), new MicrosoftAppCredentials(botClientID, botClientSecret));
            var topLevelMessageActivity = MessageFactory.Text($"I am alive!");
            var conversationParameters = new ConversationParameters
            {
                IsGroup = true,
                ChannelData = new TeamsChannelData
                {
                    Channel = new ChannelInfo(teamsChannelId),
                },
                Activity = topLevelMessageActivity
            };



            MicrosoftAppCredentials.TrustServiceUrl(serviceUrl, DateTime.MaxValue);
            try
            {
                await connectorClient.Conversations.CreateConversationAsync(conversationParameters);
            }
            catch (Exception e)
            {
                Console.WriteLine("error");
            }

        }

  

    }
}
