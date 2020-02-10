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
    public class EventChangeGraphType : ObjectGraphType<EventExtModel>
    {
        public EventChangeGraphType()
        {
            Name = "EventChange";

            Field<EventGraphType>("event",
                resolve: context => context.Source.Event
            );

            Field<UserGraphType>("actionBy",
                resolve: context => context.Source.ChangingUser
            );

            Field<EventActionGraphType>("action",
                resolve: context => context.Source.Action
            );
        }
    }
}
