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
        public async Task PushMessage<T>(string name, string title, string body, T payload = null, params string[] recipients) where T : class
        {
            var postbody = new
            {
                notification_content = new
                {
                    name,
                    title,
                    body,
                    custom_data = payload,
                    notification_target = new {
                        Type = "user_ids_target",
                        user_ids = recipients
                    }
                }
            };

            using (var client = new HttpClient())
            {
                var tokenResponseMessage = await client.SendAsync(new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri("https://appcenter.ms/api/v0.1/apps/SimonBrettschneider/Lets-play-together/push/notifications"),
                    Headers = {
                        {"X-API-Token", ""}, // todo: insert key
                        {"Accept", "application/json"}
                    },
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                });
            }
        }
    }
}
