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
    public class EventChangeType : ObjectGraphType<EventExtModel>
    {
        public EventChangeType()
        {
            Name = "EventChange";

            Field<EventType>("event",
                resolve: context => context.Source.Event
            );

            Field<UserType>("actionBy",
                resolve: context => context.Source.ChangingUser
            );

            Field<EventActionType>("action",
                resolve: context => context.Source.Action
            );
        }
    }
}
