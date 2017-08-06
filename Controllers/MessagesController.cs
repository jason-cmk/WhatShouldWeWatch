using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using WhatShouldWeWatch.Models;
using System.Collections.Generic;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using WhatShouldWeWatch.Dialogues;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using WhatShouldWeWatch.Helpers;

namespace WhatShouldWeWatch
{


    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);

                HttpClient client = new HttpClient();
                ResultModel.Result resultObject;
                string baseUri = "https://api.themoviedb.org/3";
                string themoviedbUrl = "https://www.themoviedb.org/movie/";
                string imageUrl = "https://image.tmdb.org/t/p/w600_and_h900_bestv2";
                string apiKey = "?api_key=e01be34ceffec7c3b4fc1c591884c2fc";

                Regex addFilmRegex = new Regex("^<ADD FILM>{id: [0-9]+,title: .+}$");
                Regex removeFilmRegex = new Regex("^<REMOVE FILM>{id: [0-9]+,title: .+}$");
                Match addFilmMatch = addFilmRegex.Match(activity.Text);
                Match removeFilmMatch = removeFilmRegex.Match(activity.Text);
                if (addFilmMatch.Success) // Handle "add to watch list" postback
                {
                    WatchListHelper.AddFilmAsync(activity, connector);
                } else if (removeFilmMatch.Success) // handle "remove from watchlist" postback
                {
                    WatchListHelper.RemoveFilmAsync(activity, connector);
                }
                else
                {
                    await Conversation.SendAsync(activity, () => new WhatShouldWeWatchDialogue());
                }
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}