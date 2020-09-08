using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace PlayTogetherApi.Web.GraphQl
{
    public class PlayTogetherUserContext : Dictionary<string, object>
    {
        public ClaimsPrincipal User { get; set; }
    }
}
