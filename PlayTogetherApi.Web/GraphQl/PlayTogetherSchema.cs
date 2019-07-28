using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;

namespace PlayTogetherApi.Web.GraphQl
{
    public class PlayTogetherSchema : Schema
    {
        public PlayTogetherSchema(IDependencyResolver resolver) : base(resolver)
        {
            Query = resolver.Resolve<PlayTogetherQuery>();
            Mutation = resolver.Resolve<PlayTogetherMutation>();
        }
    }
}
