using System;
using Microsoft.Extensions.DependencyInjection;
using GraphQL.Types;

namespace PlayTogetherApi.Web.GraphQl
{
    public class PlayTogetherSchema : Schema
    {
        public PlayTogetherSchema(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Query = serviceProvider.GetRequiredService<PlayTogetherQuery>();
            Mutation = serviceProvider.GetRequiredService<PlayTogetherMutation>();
            Subscription = serviceProvider.GetRequiredService<PlayTogetherSubscription>();
        }
    }
}
