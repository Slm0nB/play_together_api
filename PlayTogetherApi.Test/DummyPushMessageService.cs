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
    public class DummyPushMessageService : PushMessageService
    {
        public DummyPushMessageService() : base(null)
        {
        }

        public override Task<string> PushMessageAsync<T>(string name, string title, string body, T payload = null, params string[] recipients) where T : class
        {
            return Task.FromResult(String.Empty);
        }
    }
}
