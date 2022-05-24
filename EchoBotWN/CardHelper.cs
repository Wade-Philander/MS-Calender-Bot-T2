
using AdaptiveCards;
using EchoBotWN.Models;
using Microsoft.Graph;
using System;
using System.IO;

namespace EchoBotWN
{
    public class CardHelper
    {
        public static AdaptiveCard GetUserCard(User user, Stream photo)
        {
            // Create an Adaptive Card to display the user
            // See https://adaptivecards.io/designer/ for possibilities
            var userCard = new AdaptiveCard("1.2");

            var columns = new AdaptiveColumnSet();
            userCard.Body.Add(columns);

            var userPhotoColumn = new AdaptiveColumn { Width = AdaptiveColumnWidth.Auto };
            columns.Columns.Add(userPhotoColumn);

            userPhotoColumn.Items.Add(new AdaptiveImage
            {
                Style = AdaptiveImageStyle.Person,
                Size = AdaptiveImageSize.Small,
                Url = GetDataUriFromPhoto(photo)
            });

            var userInfoColumn = new AdaptiveColumn { Width = AdaptiveColumnWidth.Stretch };
            columns.Columns.Add(userInfoColumn);

            userInfoColumn.Items.Add(new AdaptiveTextBlock
            {
                Weight = AdaptiveTextWeight.Bolder,
                Wrap = true,
                Text = user.DisplayName
            });

            userInfoColumn.Items.Add(new AdaptiveTextBlock
            {
                Spacing = AdaptiveSpacing.None,
                IsSubtle = true,
                Wrap = true,
                Text = user.Mail ?? user.UserPrincipalName
            });

            return userCard;
        }

        private static Uri GetDataUriFromPhoto(Stream photo)
        {
            // Copy to a MemoryStream to get access to bytes
            var photoStream = new MemoryStream();
            photo.CopyTo(photoStream);

            var photoBytes = photoStream.ToArray();

            return new Uri($"data:image/png;base64,{Convert.ToBase64String(photoBytes)}");
        }

        public static AdaptiveCard GetEventCard(eventModel calendarEvent, string dateTimeFormat)
        {
            // Build an Adaptive Card for the event
            var eventCard = new AdaptiveCard("1.2");

            // Add subject as card title
            eventCard.Body.Add(new AdaptiveTextBlock
            {
                Size = AdaptiveTextSize.Medium,
                Weight = AdaptiveTextWeight.Bolder,
                Text = $"{calendarEvent.id}: {calendarEvent.subject}"
            });

            // Add details
            var details = new AdaptiveFactSet();

            details.Facts.Add(new AdaptiveFact
            {
                Title = "Message:",
                Value = calendarEvent.message //DateTime.Parse(calendarEvent.Start.DateTime).ToString(dateTimeFormat)
            }); // ;

              details.Facts.Add(new AdaptiveFact
              {
                  Title = "Date:",
                  Value = calendarEvent.date
              });

            /*  if (calendarEvent.Location != null &&
                  !string.IsNullOrEmpty(calendarEvent.Location.DisplayName))
              {
                  details.Facts.Add(new AdaptiveFact
                  {
                      Title = "Location",
                      Value = calendarEvent.Location.DisplayName
                  });
              }*/

            eventCard.Body.Add(details);

            return eventCard;
        }
    }
}
