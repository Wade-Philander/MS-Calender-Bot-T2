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
    public class NewEventDialog : LogoutDialog
    {
        protected readonly ILogger _logger;

        public NewEventDialog(
            IConfiguration configuration)
            : base(nameof(NewEventDialog), configuration["ConnectionName"])
        {

            AddDialog(new TextPrompt("subjectPrompt"));

            AddDialog(new TextPrompt("messagePrompt"));

            // Validator ensures that the input is a valid date and time
                 AddDialog(new DateTimePrompt("startPrompt", StartPromptValidatorAsync));
            // Validator ensures that the input is a valid date and time
            // and that it is later than the start
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
    PromptForSubjectAsync,
    PromptForMessageAsync,
    PromptForStartAsync,
    ConfirmNewEventAsync,
    AddEventAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        // Generate a DateTime from the list of
        // DateTimeResolutions provided by the DateTimePrompt
        private static DateTime GetDateTimeFromResolutions(IList<DateTimeResolution> resolutions)
        {
            var timex = new TimexProperty(resolutions[0].Timex);

            // Handle the "now" case
            if (timex.Now ?? false)
            {
                return DateTime.Now;
            }

            // Otherwise generate a DateTime
            return TimexHelpers.DateFromTimex(timex);
        }

        private async Task<DialogTurnResult> PromptForSubjectAsync(
    WaterfallStepContext stepContext,
    CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync("subjectPrompt",
            new PromptOptions
            {
                Prompt = MessageFactory.Text("What's the subject for your event?")
            },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForMessageAsync(
WaterfallStepContext stepContext,
CancellationToken cancellationToken)
        {
            stepContext.Values["subject"] = (string)stepContext.Result;
            return await stepContext.PromptAsync("messagePrompt",
            new PromptOptions
            {
                Prompt = MessageFactory.Text("What's the message for your event?")
            },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForStartAsync(
    WaterfallStepContext stepContext,
    CancellationToken cancellationToken)
        {
            stepContext.Values["message"] = (string)stepContext.Result;

            return await stepContext.PromptAsync("startPrompt",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("When does the event start?"),
                    RetryPrompt = MessageFactory.Text("I'm sorry, I didn't get that. Please provide both a day and a time.")
                },
                cancellationToken);
        }



        private async Task<DialogTurnResult> ConfirmNewEventAsync(
        WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
        {

            var dateTimes = stepContext.Result as IList<DateTimeResolution>;

            var start = GetDateTimeFromResolutions(dateTimes);

            stepContext.Values["start"] = start;

      
            var subject = stepContext.Values["subject"] as string;
            var message = stepContext.Values["message"] as string;

            var date = stepContext.Values["start"] as DateTime?;


            // Build a Markdown string
            var markdown = "Here's what I heard:\n\n";
            markdown += $"- **Subject:** {subject}\n";
            markdown += $"- **Message:** {message}\n";
            markdown += $"- **Date:** {date}";

            await stepContext.Context.SendActivityAsync(
                MessageFactory.Text(markdown));

            return await stepContext.PromptAsync(nameof(ConfirmPrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Is this correct?")
                },
                cancellationToken);
        }


        private async Task<DialogTurnResult> AddEventAsync(
        WaterfallStepContext stepContext,
        CancellationToken cancellationToken)
        {
            var date = stepContext.Values["start"] as DateTime?;
            // Initialize an Event object
            var newEvent = new eventModel("0",stepContext.Values["subject"] as string, stepContext.Values["message"] as string, date.ToString());
           


            dynamic result = await excel.addEvent(newEvent);


            await stepContext.Context.SendActivityAsync(
                        MessageFactory.Text("Event successfully added"),
                        cancellationToken);

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }





        private static bool TimexHasDateAndTime(TimexProperty timex)
        {
            return timex.Now ?? false ||
                (timex.Types.Contains(TimexTypes.DateTime) &&
                timex.Types.Contains(TimexTypes.Definite));
        }

        private static Task<bool> StartPromptValidatorAsync(
            PromptValidatorContext<IList<DateTimeResolution>> promptContext,
            CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded)
            {
                // Initialize a TimexProperty from the first
                // recognized value
                var timex = new TimexProperty(
                    promptContext.Recognized.Value[0].Timex);

                // If it has a definite date and time, it's valid
                return Task.FromResult(TimexHasDateAndTime(timex));
            }

            return Task.FromResult(false);
        }

        private static Task<bool> EndPromptValidatorAsync(
    PromptValidatorContext<IList<DateTimeResolution>> promptContext,
    CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded)
            {
                if (promptContext.Options.Validations is DateTime start)
                {
                    // Initialize a TimexProperty from the first
                    // recognized value
                    var timex = new TimexProperty(
                        promptContext.Recognized.Value[0].Timex);

                    // Get the DateTime from this value to compare with start
                    var end = GetDateTimeFromResolutions(promptContext.Recognized.Value);

                    // If it has a definite date and time, and
                    // the value is later than start, it's valid
                    return Task.FromResult(TimexHasDateAndTime(timex) &&
                        DateTime.Compare(start, end) < 0);
                }
            }

            return Task.FromResult(false);
        }
    }
}