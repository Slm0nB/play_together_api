﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameCalendarApi.Web.Models
{
    public class TokenRequestModel
    {
        public string Grant_type { get; set; }
        public string Scope { get; set; }

        // for the "password" grant type
        public string Username { get; set; }
        public string Password { get; set; }

        // for the "refresh_token" grant type
        public Guid? Refresh_token { get; set; }

        // for the "api_key" grant type
        public Guid? Api_key { get; set; }

    }
}
