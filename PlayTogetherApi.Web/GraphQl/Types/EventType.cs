using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using PlayTogetherApi.Domain;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class EventType : ObjectGraphType<Event>
    {
        public EventType(PlayTogetherDbContext db)
        {
            Field("id", x => x.EventId, type: typeof(IdGraphType)).Description("Id property from the event object.");
            Field(x => x.CreatedDate, type: typeof(NonNullGraphType<DateTimeGraphType>)).Description("When the event was created.");
            Field<DateTimeGraphType>("startDate", resolve: context => context.Source.EventDate, description: "When the event starts.");
            Field<DateTimeGraphType>("endDate", resolve: context => context.Source.EventEndDate, description: "When the event ends.");
            Field(x => x.EventEndDate, type: typeof(DateTimeGraphType)).Description("When the event ends.");
            Field(x => x.Title).Description("Title of the event.");
            Field(x => x.Description, type: typeof(StringGraphType)).Description("Description of the event.");

            FieldAsync<UserType>("author",
                resolve: async context => context.Source.CreatedByUser ?? await db.Users.FirstOrDefaultAsync(n => n.UserId == context.Source.CreatedByUserId)
            );

            FieldAsync<GameType>("game",
                resolve: async context => context.Source.Game ?? await db.Games.FirstOrDefaultAsync(n => n.GameId == context.Source.GameId)
            );

            FieldAsync<ListGraphType<UserEventSignupType>>("signups",
                arguments: new QueryArguments(
                   new QueryArgument<DateTimeGraphType> { Name = "beforeDate", Description = "Event occurs before or on this datetime." },
                   new QueryArgument<DateTimeGraphType> { Name = "afterDate", Description = "Event occurs on or after this datetime." },
                   new QueryArgument<IntGraphType> { Name = "skip", Description = "How many events to skip." },
                   new QueryArgument<IntGraphType> { Name = "take", Description = "How many events to return." }
                ),
                resolve: async context =>
                {
                    var eventId = context.Source.EventId;
                    var signups = await db.UserEventSignups
                        .Where(n => n.EventId == eventId)
                        .Include(n => n.User)
                        .OrderBy(n => n.SignupDate)
                        .ToListAsync();

                    var afterDate = context.GetArgument<DateTime>("afterDate");
                    if (afterDate != default(DateTime))
                    {
                        signups = signups.Where(n => n.Event.EventEndDate >= afterDate).ToList();
                    }

                    var beforeDate = context.GetArgument<DateTime>("beforeDate");
                    if (beforeDate != default(DateTime))
                    {
                        signups = signups.Where(n => n.Event.EventDate <= beforeDate).ToList();
                    }

                    var skip = context.GetArgument<int>("skip");
                    if (skip > 0)
                    {
                        signups = signups.Skip(skip).ToList();
                    }

                    var take = context.GetArgument<int>("take");
                    if (take > 0)
                    {
                        signups = signups.Take(take).ToList();
                    }

                    return signups;
                }
            );
        }
    }
}
