using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;
using GameCalendarApi.Domain;

namespace GameCalendarApi.Web.GraphQl.Types
{
    public class EventType : ObjectGraphType<Event>
    {
        public EventType(GameCalendarDbContext db)
        {
            Field("id", x => x.EventId, type: typeof(IdGraphType)).Description("Id property from the event object.");
            Field(x => x.CreatedDate, type: typeof(IdGraphType)).Description("CreatedDate property from the event object.");
            Field(x => x.Title).Description("Title property from the event object.");

            Field<ListGraphType<UserType>>("author",
                resolve: x => db.Users.Where(n => n.UserId == x.Source.CreatedByUserId)
            );
        }
    }
}
