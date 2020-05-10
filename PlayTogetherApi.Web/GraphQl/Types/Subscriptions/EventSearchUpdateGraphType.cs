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
    public class EventSearchUpdateGraphType : ObjectGraphType<EventSearchUpdateModel>
    {
        public EventSearchUpdateGraphType()
        {
            Name = "EventSearchUpdate";

            Field<ListGraphType<EventGraphType>>("added",
                resolve: context => context.Source.Added
            );

            Field<ListGraphType<EventGraphType>>("removed",
                resolve: context => context.Source.Removed
            );
        }
    }
}
