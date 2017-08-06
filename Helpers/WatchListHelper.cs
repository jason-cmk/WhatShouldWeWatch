using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using WhatShouldWeWatch.Models;

namespace WhatShouldWeWatch.Helpers
{
    public class WatchListHelper
    {
        const string BASE_URI = "https://api.themoviedb.org/3";
        const string API_KEY = "?api_key=e01be34ceffec7c3b4fc1c591884c2fc";
        const string THE_MOVIE_DB_URL = "https://www.themoviedb.org/movie/";
        const string IMAGE_URL = "https://image.tmdb.org/t/p/w600_and_h900_bestv2";

        public static async void AddFilmAsync(Activity activity, ConnectorClient connector)
        {
            HttpClient client = new HttpClient();
            string film = activity.Text.Substring(10);
            JObject filmJson = JObject.Parse(film);
            string filmTitle = (string)filmJson["title"];
            int filmId = (int)filmJson["id"];
            Activity reply = activity.CreateReply($"Adding \"{filmTitle}\" to your watch list..");
            await connector.Conversations.SendToConversationAsync(reply);

            UserResult userResultModel = new UserResult
            {
                userId = activity.From.Id,
                resultId = filmId
            };

            await AzureManager.AzureManagerInstance.AddUserResult(userResultModel);
            reply = activity.CreateReply($"It's done! Here is your to-watch list:");

            List<UserResult> userResultList = await AzureManager.AzureManagerInstance.GetUserResults();
            List<UserResult> personalizedList = userResultList.Where(o => o.userId.Equals(activity.From.Id)).ToList();

            // Display to-watch list
            reply.AttachmentLayout = "carousel";
            reply.Attachments = new List<Attachment>();
            foreach (UserResult userResult in personalizedList)
            {
                string apiQuery = await client.GetStringAsync(new Uri(
                    BASE_URI
                    + $"/movie/{userResult.resultId}"
                    + API_KEY
                ));


                ResultModel.Result resultObject = JsonConvert.DeserializeObject<ResultModel.Result>(apiQuery);
                List<CardImage> cardImages = new List<CardImage>();
                List<CardAction> cardButtons = new List<CardAction>();
                CardImage ci = new CardImage($"{IMAGE_URL}{resultObject.poster_path}");
                cardImages.Add(ci);
                CardAction ca = new CardAction()
                {
                    Title = resultObject.title,
                    Type = "openUrl",
                    Value = $"{THE_MOVIE_DB_URL}{resultObject.id}"
                };
                CardAction button = new CardAction()
                {
                    Title = $"Remove from watch list",
                    Type = "postBack",
                    Value = "<REMOVE FILM>{"
                            + $"id: {resultObject.id},"
                            + $"title: \"{resultObject.title}\""
                            + "}"
                };
                cardButtons.Add(button);
                ThumbnailCard tc = new ThumbnailCard()
                {
                    Title = resultObject.title,
                    Subtitle = resultObject.overview,
                    Images = cardImages,
                    Tap = ca,
                    Buttons = cardButtons
                };
                reply.Attachments.Add(tc.ToAttachment());
            }

            await connector.Conversations.SendToConversationAsync(reply);
        }

        public static async void RemoveFilmAsync(Activity activity, ConnectorClient connector)
        {
            HttpClient client = new HttpClient();
            string film = activity.Text.Substring(13);
            JObject filmJson = JObject.Parse(film);
            string filmTitle = (string)filmJson["title"];
            int filmId = (int)filmJson["id"];
            Activity reply = activity.CreateReply($"Removing \"{filmTitle}\" from your watch list..");
            await connector.Conversations.SendToConversationAsync(reply);

            UserResult userResultModel = new UserResult
            {
                userId = activity.From.Id,
                resultId = filmId
            };

            await connector.Conversations.SendToConversationAsync(reply);

            List<UserResult> userResultList = await AzureManager.AzureManagerInstance.GetUserResults();
            List<UserResult> searchResultList = userResultList
                .Where(o => o.resultId == filmId)
                .Where(o => o.userId.Equals(activity.From.Id))
                .ToList();
            List<UserResult> personalizedList = userResultList
                .Where(o => o.userId.Equals(activity.From.Id))
                .ToList();

            if (searchResultList.Count > 0)
            {
                UserResult toBeDeleted = searchResultList[0];
                await AzureManager.AzureManagerInstance.DeleteUserResult(toBeDeleted);
                reply = activity.CreateReply($"Deleted {toBeDeleted.id}! Here is your to-watch list:");

                // Display to-watch list
                reply.AttachmentLayout = "carousel";
                reply.Attachments = new List<Attachment>();
                foreach (UserResult userResult in personalizedList)
                {
                    string apiQuery = await client.GetStringAsync(new Uri(
                        BASE_URI
                        + $"/movie/{userResult.resultId}"
                        + API_KEY
                    ));

                    ResultModel.Result resultObject = JsonConvert.DeserializeObject<ResultModel.Result>(apiQuery);
                    List<CardImage> cardImages = new List<CardImage>();
                    List<CardAction> cardButtons = new List<CardAction>();
                    CardImage ci = new CardImage($"{IMAGE_URL}{resultObject.poster_path}");
                    cardImages.Add(ci);
                    CardAction ca = new CardAction()
                    {
                        Title = resultObject.title,
                        Type = "openUrl",
                        Value = $"{THE_MOVIE_DB_URL}{resultObject.id}"
                    };
                    CardAction button = new CardAction()
                    {
                        Title = $"Watched it",
                        Type = "postBack",
                        Value = "<REMOVE FILM>{"
                                + $"id: {resultObject.id},"
                                + $"title: \"{resultObject.title}\""
                                + "}"
                    };
                    cardButtons.Add(button);
                    ThumbnailCard tc = new ThumbnailCard()
                    {
                        Title = resultObject.title,
                        Subtitle = resultObject.overview,
                        Images = cardImages,
                        Tap = ca,
                        Buttons = cardButtons
                    };
                    reply.Attachments.Add(tc.ToAttachment());
                }
            }
            else
            {
                reply = activity.CreateReply("Oh no, something went wrong :(");
            }

            await connector.Conversations.SendToConversationAsync(reply);
        }
    }
}