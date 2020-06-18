using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using PlayTogetherApi.Web.Models;
using PlayTogetherApi.Data;
using PlayTogetherApi.Services;

namespace PlayTogetherApi.Web.Controllers
{
    [ApiController]
    public class TestController : ControllerBase
    {
        /*
        [AllowAnonymous]
        [HttpGet]
        [Route("api/testkey")]
        public async Task<IActionResult> TestKey([FromServices] IConfiguration conf)
        {
            var apiKey = "key: " + conf.GetSection("PlayTogetherPushKey").Value;
            return Ok(apiKey);
        }
        */

        /*
        [AllowAnonymous]
        [HttpGet]
        [Route("api/testpush")]
        public async Task<IActionResult> TestPush([FromServices] PushMessageService service)
        {
            await service.PushMessageAsync("event name", "event title", "body", new { type = "JoinEvent " }, "fw4Wj8eI6GU:APA91bFA5mJECKH7rK5rubwtNEzPxSCBI9yVf5LoKMgmBeFQEJy6xQAq_jsw3ehm4PRIxzBH5Pq4xKPrc-aYRe72g6bz3xf17A44K-CzGqN7bsQm5jFFaA4J2fOa6cguiVPGkt7uUUzP");

            return Ok();
        }
        */
    }
}
