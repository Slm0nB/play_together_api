using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
            try
            {
                recipients = recipients?.Where(n => !string.IsNullOrWhiteSpace(n)).ToArray();

                if (payload == null || recipients == null || recipients.Length == 0)
                    return string.Empty;

                var postbody = new
                {
                    notification = new
                    {
                        title = title,
                        body = body,
                    },
                    data = payload,
                    content_available = true,
                    registration_ids = recipients.Where(n => !string.IsNullOrWhiteSpace(n)).ToArray()
                };

                var json = JsonConvert.SerializeObject(postbody);

                var message = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri("https://fcm.googleapis.com/fcm/send"),
                    Headers = {
                        {"Accept", "application/json"}
                    },
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                message.Headers.Authorization = new AuthenticationHeaderValue("key", "=" + ApiKey);

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
            catch(Exception ex)
            {
                throw;
            }
        }
    }
}
