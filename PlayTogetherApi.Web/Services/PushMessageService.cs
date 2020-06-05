using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PlayTogetherApi.Services
{
    public class PushMessageService
    {
        private readonly string ApiKey;

        public PushMessageService (IConfiguration conf)
        {
            ApiKey = conf?.GetSection("PlayTogetherPushKey").Value;
        }

        public async Task<string> PushMessageAsync<T>(string name, string title, string body, T payload = null, params string[] recipients) where T : class
        {
            var postbody = new
            {
                notification_content = new
                {
                    name,
                    title,
                    body,
                    custom_data = payload
                },
                notification_target = new
                {
                    type = "user_ids_target",
                    user_ids = recipients
                }
            };

            var json = JsonConvert.SerializeObject(postbody);

            var message = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://appcenter.ms/api/v0.1/apps/SimonBrettschneider/Lets-play-together/push/notifications"),
                Headers = {
                        {"X-API-Token", ApiKey},
                        {"Accept", "application/json"}
                    },
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using (var client = new HttpClient())
            {
                using (var responseMessage = await client.SendAsync(message))
                {
                    using (HttpContent content = responseMessage.Content)
                    {
                        var result = await content.ReadAsStringAsync();
                        return result;

                        // todo: might instead parse the json and return the messageid guid
                    }
                }
            }
        }
    }
}
