using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;
using GameCalendarApi.Domain;

namespace GameCalendarApi.Web.GraphQl.Types
{
    public class EventInputType : InputObjectGraphType<Event>
    {
        public EventInputType()
        {
            Name = "EventInput";
            Field(x => x.Title);
            Field<Guid>("authorId", x => x.CreatedByUserId, type: typeof(IdGraphType), nullable: false);
        }
    }
}
