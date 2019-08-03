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
            Field(x => x.CreatedDate, type: typeof(NonNullGraphType<DateTimeGraphType>)).Description("CreatedDate property from the event object.");
            Field(x => x.EventDate, type: typeof(DateTimeGraphType)).Description("EventDate property from the event object.");
            Field(x => x.Title).Description("Title property from the event object.");
            Field(x => x.Description, type: typeof(StringGraphType)).Description("Description property from the event object.");

            FieldAsync<UserType>("author",
                resolve: async context => context.Source.CreatedByUser ?? await db.Users.FirstOrDefaultAsync(n => n.UserId == context.Source.CreatedByUserId)
            );

            FieldAsync<GameType>("game",
                resolve: async context => context.Source.Game ?? await db.Games.FirstOrDefaultAsync(n => n.GameId == context.Source.GameId)
            );

            FieldAsync<ListGraphType<UserType>>("signups",
                resolve: async context =>
                {
                    var eventId = context.Source.EventId;
                    var users = await db.UserEventSignups
                        .Where(n => n.EventId == eventId)
                        .Include(n => n.User)
                        .OrderBy(n => n.SignupDate)
                        .Select(n => n.User)
                        .ToListAsync();
                    return users;
                }
            );
        }
    }
}
