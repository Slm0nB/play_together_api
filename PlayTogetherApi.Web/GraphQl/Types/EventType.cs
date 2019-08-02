using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;
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

            Field<UserType>("author",
                resolve: x => x.Source.CreatedByUser ?? db.Users.FirstOrDefault(n => n.UserId == x.Source.CreatedByUserId)
            );

            Field<GameType>("game",
                resolve: x => x.Source.Game ?? db.Games.FirstOrDefault(n => n.GameId == x.Source.GameId)
            );
        }
    }
}
