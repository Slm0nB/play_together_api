using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;

namespace GameCalendarApi.Web.GraphQl
{
    public class GameCalendarSchema : Schema
    {
    public GameCalendarSchema(IDependencyResolver resolver) : base(resolver)
    {
        Query = resolver.Resolve<GameCalendarQuery>();
    }
}
}
