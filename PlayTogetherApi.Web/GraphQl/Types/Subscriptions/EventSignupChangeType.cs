using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using PlayTogetherApi.Data;
using PlayTogetherApi.Services;
using PlayTogetherApi.Web.Models;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class EventSignupChangeType : UserEventSignupType
    {
        public EventSignupChangeType(PlayTogetherDbContext db) : base(db)
        {
            Name = "EventSignupChange";

            // Doesn't need to do anything yet; just formally make it a different type than the raw signup
        }
    }
}
