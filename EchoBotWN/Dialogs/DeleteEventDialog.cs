using EchoBotWN.Excel;
using EchoBotWN.Graph;
using EchoBotWN.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TimexTypes = Microsoft.Recognizers.Text.DataTypes.TimexExpression.Constants.TimexTypes;

namespace EchoBotWN.Dialogs
{
    public class DeleteEventDialog : LogoutDialog
    {
        protected readonly ILogger _logger;

        public DeleteEventDialog(
            IConfiguration configuration)
            : base(nameof(DeleteEventDialog), configuration["ConnectionName"])
        {

            AddDialog(new TextPrompt("deletePrompt"));

            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                PromptForDeleteAsync,
                ConfirmDeleteEventAsync,
                DeleteEventAsync
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> PromptForDeleteAsync(
    WaterfallStepContext stepContext,
    CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync("deletePrompt",
            new PromptOptions
            {
                Prompt = MessageFactory.Text("Please input the event id")
            },
                cancellationToken);
        }


        private async Task<DialogTurnResult> ConfirmDeleteEventAsync(
        WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
        {
            stepContext.Values["id"] = (string)stepContext.Result;
            var id = stepContext.Values["id"] as string;

            eventModel newEvent = await excel.getEvent(int.Parse(id));

            var markdown = "Event:\n\n";
              markdown += $"- **Subject:** {newEvent.subject}\n";
              markdown += $"- **Message:** {newEvent.message}\n";
              markdown += $"- **Date:** {newEvent.date}"; 

            await stepContext.Context.SendActivityAsync(
                MessageFactory.Text(markdown));

            return await stepContext.PromptAsync(nameof(ConfirmPrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Confirm you want to delete this event?")
                },
                cancellationToken);
        }


        private async Task<DialogTurnResult> DeleteEventAsync(
        WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
        {
            var id = stepContext.Values["id"] as string;


            dynamic result = await excel.deleteEvent(int.Parse(id));


            await stepContext.Context.SendActivityAsync(
                        MessageFactory.Text("Event successfully deleted"),
                        cancellationToken);

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
  
    }
}