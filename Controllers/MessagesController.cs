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

                // Stateful variables
                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                userData.SetProperty<List<int>>("seen", new List<int>());
                string message = activity.Text;

                string endOutput = "Hi there, don't know what we should watch? Give me something to work off (a movie)";
                bool greeted = userData.GetProperty<bool>("Greeted");
                if (greeted)
                {
                    int queryCount = userData.GetProperty<int>("QueryCount");

                    if (message.ToLower().Contains("clear") || message.ToLower().Contains("reset"))
                    {
                        endOutput = "Ending conversation. Give me a movie to start again.";
                        await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                    }
                    else // if querying
                    {
                        string baseUri = "https://api.themoviedb.org/3";
                        string credentials = "?api_key=e01be34ceffec7c3b4fc1c591884c2fc";
                        ResultModel.RootObject rootObject;
                        HttpClient client = new HttpClient();
                        ResultModel.Result first;
                        ResultModel.Result second;
                        List<int> seen = new List<int>();

                        if (queryCount == 0)
                        {
                            string x = await client.GetStringAsync(new Uri(
                                baseUri
                                + "/search/movie"
                                + credentials
                                + "&query="
                                + message
                                ));
                            rootObject = JsonConvert.DeserializeObject<ResultModel.RootObject>(x);

                            if (rootObject.total_results > 1)
                            {
                                rootObject.results = rootObject.results.OrderByDescending(o => o.popularity).ToList();
                                first = rootObject.results[0];
                                second = rootObject.results[1];
                                userData.SetProperty<ResultModel.Result>("First", first);
                                userData.SetProperty<ResultModel.Result>("Second", second);
                                endOutput = $"Would you rather watch {first.title} or {second.title}?";

                                seen = userData.GetProperty<List<int>>("seen");
                                seen.Add(first.id);
                                seen.Add(second.id);
                                userData.SetProperty<List<int>>("seen", seen);

                                userData.SetProperty<int>("QueryCount", queryCount + 1);
                                // save user data to bot state
                                await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            }
                            else
                            {
                                endOutput = $"I would recommend {rootObject.results[0].title}";
                                await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                            }
                            userData.SetProperty<int>("QueryCount", queryCount + 1);

                        }
                        else if (queryCount < 3)
                        {
                            first = userData.GetProperty<ResultModel.Result>("First");
                            second = userData.GetProperty<ResultModel.Result>("Second");
                            int searchId = 0;
                            if (message.ToLower().Equals(first.title.ToLower()))
                            {
                                searchId = first.id;
                            }
                            else if (message.ToLower().Equals(second.title.ToLower()))
                            {
                                searchId = second.id;
                            }
                            else
                            {
                                string search = await client.GetStringAsync(new Uri(
                                baseUri
                                + "/search/movie"
                                + credentials
                                + "&query="
                                + message
                                ));
                                rootObject = JsonConvert.DeserializeObject<ResultModel.RootObject>(search);
                                searchId = rootObject.results.OrderByDescending(o => o.popularity).ToList()[0].id;
                            }

                            string x = await client.GetStringAsync(new Uri(
                                baseUri
                                + $"/movie/{searchId}/similar"
                                + credentials
                                ));
                            rootObject = JsonConvert.DeserializeObject<ResultModel.RootObject>(x);
                            rootObject.results = rootObject.results.OrderByDescending(o => o.popularity).ToList();

                            if (rootObject.results.Count > 1)
                            {
                                first = rootObject.results[0];
                                second = rootObject.results[1];

                                userData.SetProperty<ResultModel.Result>("First", first);
                                userData.SetProperty<ResultModel.Result>("Second", second);
                                endOutput = $"Would you rather watch {first.title} or {second.title}?";
                                userData.SetProperty<int>("QueryCount", queryCount + 1);

                                seen = userData.GetProperty<List<int>>("seen");
                                seen.Add(first.id);
                                seen.Add(second.id);
                                userData.SetProperty<List<int>>("seen", seen);
                            }
                            else
                            {
                                endOutput = $"I would recommend {rootObject.results[0].title}";
                                await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                            }

                            // save user data to bot state
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        }
                        else
                        {
                            first = userData.GetProperty<ResultModel.Result>("First");
                            second = userData.GetProperty<ResultModel.Result>("Second");
                            int searchId;
                            if (message.ToLower().Equals(first.title.ToLower()))
                            {
                                searchId = first.id;
                            }
                            else if (message.ToLower().Equals(second.title.ToLower()))
                            {
                                searchId = second.id;
                            }
                            else
                            {
                                string search = await client.GetStringAsync(new Uri(
                                baseUri
                                + "/search/movie"
                                + credentials
                                + "&query="
                                + message
                                ));
                                rootObject = JsonConvert.DeserializeObject<ResultModel.RootObject>(search);
                                searchId = rootObject.results.OrderByDescending(o => o.popularity).ToList()[0].id;
                            }
                            string x = await client.GetStringAsync(new Uri(
                                baseUri
                                + $"/movie/{searchId}/recommendations"
                                + credentials
                                ));
                            rootObject = JsonConvert.DeserializeObject<ResultModel.RootObject>(x);

                            //bool found = false;
                            int i = 0;
                            seen = userData.GetProperty<List<int>>("seen");
                            rootObject.results = rootObject.results.OrderByDescending(o => o.popularity).ToList();
                            //while (!found || i < rootObject.results.Count)
                            //{
                            //    if (seen.IndexOf(rootObject.results[i].id) == -1)
                            //    {
                            //        found = true;
                            //    }
                            //    i++;
                            //}
                            endOutput = $"I would recommend {rootObject.results[i].title}";
                            await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                        }

                        
                    }
                }
                else
                {
                    userData.SetProperty<bool>("Greeted", true);
                    // save user data to bot state
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                }
                
                // return our reply to the user
                Activity reply = activity.CreateReply(endOutput);
                await connector.Conversations.ReplyToActivityAsync(reply);

                
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