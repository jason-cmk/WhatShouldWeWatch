using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using WhatShouldWeWatch.Models;

namespace WhatShouldWeWatch.Dialogues
{
    [LuisModel("ff8be190-a85b-4eef-b412-414bf68d458d", "3247027198aa4e78ba73a1cbbfefcf07")]
    [Serializable]
    public class WhatShouldWeWatchDialogue : LuisDialog<object>
    {
        [LuisIntent("Greeting")]
        public async Task Greeting(IDialogContext context, LuisResult result)
        {
            string replyMessage = "Hello! Name a film that is similar to what you feel like watching and I can help you out";
            await context.PostAsync(replyMessage);
            context.Wait(MessageReceived);
        }

        [LuisIntent("AskFilm")]
        public async Task AskFilm(IDialogContext context, LuisResult result)
        {
            string replyMessage = "";
            HttpClient client = new HttpClient();
            string themoviedbUrl = "https://www.themoviedb.org/movie/";
            string baseUri = "https://api.themoviedb.org/3";
            string imageUrl = "https://image.tmdb.org/t/p/w600_and_h900_bestv2";
            string apiKey = "?api_key=e01be34ceffec7c3b4fc1c591884c2fc";
            EntityRecommendation rec;
            ResultModel.RootObject rootObject = new ResultModel.RootObject();

            if (result.TryFindEntity("Film", out rec))
            {
                string filmName = rec.Entity;
                replyMessage = $"Ok, let me think..";
                await context.PostAsync(replyMessage);
                Console.WriteLine(filmName);
                string searchQuery = await client.GetStringAsync(new Uri(
                            baseUri
                            + "/search/movie"
                            + apiKey
                            + "&query="
                            + filmName
                            ));
                rootObject = JsonConvert.DeserializeObject<ResultModel.RootObject>(searchQuery);
                if (rootObject.total_results > 0)
                {
                    int searchFilmId = rootObject.results[0].id;

                    string recommendationQuery = await client.GetStringAsync(new Uri(
                            baseUri
                            + $"/movie/{searchFilmId}/recommendations"
                            + apiKey
                    ));

                    rootObject = JsonConvert.DeserializeObject<ResultModel.RootObject>(recommendationQuery);
                    int cardsLength;
                    if (rootObject.total_results > 3)
                    {
                        cardsLength = 3;
                    } else if (rootObject.total_results > 0 && rootObject.total_results < 4) 
                    {
                        cardsLength = rootObject.total_results;
                    } else
                    {
                        cardsLength = 0;
                    }

                    var reply = context.MakeMessage();
                    reply.Attachments = new List<Attachment>();
                    for (int i=0; i<cardsLength; i++)
                    {
                        List<CardImage> cardImages = new List<CardImage>();
                        ResultModel.Result resultFilm = rootObject.results[i];
                        CardImage ci = new CardImage($"{imageUrl}{resultFilm.poster_path}");
                        cardImages.Add(ci);
                        CardAction ca = new CardAction()
                        {
                            Title = resultFilm.title,
                            Type = "openUrl",
                            Value = $"{themoviedbUrl}{resultFilm.id}"
                        };
                        ThumbnailCard tc = new ThumbnailCard()
                        {
                            Title = resultFilm.title,
                            Subtitle = resultFilm.overview,
                            Images = cardImages,
                            Tap = ca
                        };
                        reply.Attachments.Add(tc.ToAttachment());
                    }
                    
                    // send the cards
                    await context.PostAsync(reply);
                    context.Wait(MessageReceived);
                }
            } else // No film entity detected
            {
                replyMessage = "Please name a film for me to work off";
            }

            await context.PostAsync(replyMessage);
            context.Wait(MessageReceived);
        }

        [LuisIntent("NameFilm")]
        public async Task NameFilm(IDialogContext context, LuisResult result)
        {
            string replyMessage = "";
            HttpClient client = new HttpClient();
            string themoviedbUrl = "https://www.themoviedb.org/movie/";
            string baseUri = "https://api.themoviedb.org/3";
            string imageUrl = "https://image.tmdb.org/t/p/w600_and_h900_bestv2";
            string apiKey = "?api_key=e01be34ceffec7c3b4fc1c591884c2fc";
            EntityRecommendation rec;
            ResultModel.RootObject rootObject = new ResultModel.RootObject();
            string filmName;

            if (result.TryFindEntity("Film", out rec))
            {
                filmName = rec.Entity;
            } else
            {
                filmName = result.Query;
            }

            replyMessage = $"Ok, let me think..";
            await context.PostAsync(replyMessage);
            Console.WriteLine(filmName);
            string searchQuery = await client.GetStringAsync(new Uri(
                        baseUri
                        + "/search/movie"
                        + apiKey
                        + "&query="
                        + filmName
                        ));
            rootObject = JsonConvert.DeserializeObject<ResultModel.RootObject>(searchQuery);
            if (rootObject.total_results > 0)
            {
                int searchFilmId = rootObject.results[0].id;

                string recommendationQuery = await client.GetStringAsync(new Uri(
                        baseUri
                        + $"/movie/{searchFilmId}/recommendations"
                        + apiKey
                ));

                rootObject = JsonConvert.DeserializeObject<ResultModel.RootObject>(recommendationQuery);
                int cardsLength;
                replyMessage = "This is what I came up with";
                if (rootObject.total_results > 3)
                {
                    cardsLength = 3;
                }
                else if (rootObject.total_results > 0 && rootObject.total_results < 4)
                {
                    cardsLength = rootObject.total_results;
                }
                else
                {
                    replyMessage = "Sorry, I couldn't find anything similar :(\nFeel free to try another film";
                    cardsLength = 0;
                }

                var reply = context.MakeMessage();
                reply.Attachments = new List<Attachment>();
                for (int i = 0; i < cardsLength; i++)
                {
                    List<CardImage> cardImages = new List<CardImage>();
                    ResultModel.Result resultFilm = rootObject.results[i];
                    CardImage ci = new CardImage($"{imageUrl}{resultFilm.poster_path}");
                    cardImages.Add(ci);
                    CardAction ca = new CardAction()
                    {
                        Title = resultFilm.title,
                        Type = "openUrl",
                        Value = $"{themoviedbUrl}{resultFilm.id}"
                    };
                    ThumbnailCard tc = new ThumbnailCard()
                    {
                        Title = resultFilm.title,
                        Subtitle = resultFilm.overview,
                        Images = cardImages,
                        Tap = ca
                    };
                    reply.Attachments.Add(tc.ToAttachment());
                }

                // send the cards
                await context.PostAsync(replyMessage);
                await context.PostAsync(reply);
                context.Wait(MessageReceived);
            }
        }

        [LuisIntent("")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            string replyMessage = $"Sorry, not sure what that means";

            await context.PostAsync(replyMessage);
            context.Wait(MessageReceived);
        }
    }
}