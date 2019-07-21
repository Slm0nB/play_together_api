using System;
using System.Linq;
using GraphQL.Types;
using GameCalendarApi.Domain;

namespace GameCalendarApi.Web.GraphQl
{
    public class UserType : ObjectGraphType<User>
    {
        public UserType()
        {
            Field(x => x.UserId, type: typeof(IdGraphType)).Description("Id property from the user object.");
            Field(x => x.DisplayName).Description("DisplayName property from the user object.");
        }
    }

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

    public class GameCalendarQuery : ObjectGraphType
    {
        public GameCalendarQuery(GameCalendarDbContext db)
        {
            Field<ListGraphType<EventType>>(
               "events",
               resolve: context => db.Events
           );
        }
    }
}
