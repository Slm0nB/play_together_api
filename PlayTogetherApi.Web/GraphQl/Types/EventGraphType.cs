using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using PlayTogetherApi.Data;
using PlayTogetherApi.Web.Models;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class EventGraphType : EventBaseGraphType
    {
        public EventGraphType(PlayTogetherDbContext db) : base(db)
        {
            Name = "Event";

            Field<UserEventSignupCollectionGraphType>("signups",
                arguments: new QueryArguments(
                   new QueryArgument<DateTimeGraphType> { Name = "beforeDate", Description = "Event occurs before or on this datetime." },
                   new QueryArgument<DateTimeGraphType> { Name = "afterDate", Description = "Event occurs on or after this datetime." },
                   new QueryArgument<IntGraphType> { Name = "skip", Description = "How many events to skip." },
                   new QueryArgument<IntGraphType> { Name = "take", Description = "How many events to return." }
                ),
                resolve: context =>
                {
                    var eventId = context.Source.EventId;
                    IQueryable<UserEventSignup> signups = db.UserEventSignups
                        .Where(n => n.EventId == eventId)
                        .Include(n => n.User)
                        .OrderBy(n => n.SignupDate);

                    var afterDate = context.GetArgument<DateTime>("afterDate");
                    if (afterDate != default(DateTime))
                    {
                        signups = signups.Where(n => n.Event.EventEndDate >= afterDate);
                    }

                    var beforeDate = context.GetArgument<DateTime>("beforeDate");
                    if (beforeDate != default(DateTime))
                    {
                        signups = signups.Where(n => n.Event.EventDate <= beforeDate);
                    }

                    var skip = context.GetArgument<int>("skip");
                    if (skip > 0)
                    {
                        signups = signups.Skip(skip);
                    }

                    var take = context.GetArgument<int>("take");
                    if (take > 0)
                    {
                        signups = signups.Take(take);
                    }

                    return new UserEventSignupCollectionModel
                    {
                        ItemsQuery = signups,
                        TotalItemsQuery = db.UserEventSignups.Where(n => n.EventId == context.Source.EventId)
                    };
                }
            );
        }
    }
}
