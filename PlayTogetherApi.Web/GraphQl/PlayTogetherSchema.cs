using System;
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
            Subscription = resolver.Resolve<PlayTogetherSubscription>();
        }
    }
}
