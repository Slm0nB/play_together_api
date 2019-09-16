using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using GraphQL;
using GraphQL.Types;
using GraphQL.Subscription;
using GraphQL.Resolvers;
using PlayTogetherApi.Services;
using PlayTogetherApi.Domain;
using PlayTogetherApi.Web.Models;
using PlayTogetherApi.Web.GraphQl.Types;

namespace PlayTogetherApi.Web.GraphQl
{
    public class PlayTogetherSubscription : ObjectGraphType
    {
        SubscriptionObservables observables;

        public PlayTogetherSubscription(PlayTogetherDbContext db, AuthenticationService authenticationService, SubscriptionObservables observables)
        {
            this.observables = observables;

            Name = "Subscription";

            AddField(new EventStreamFieldType
            {
                // todo: add arguments for filtering
                Name = "changedEvents",
                Type = typeof(EventBaseType),
                Resolver = new FuncFieldResolver<Event>(ResolveMessage),
                Subscriber = new EventStreamResolver<Event>(SubscribeAll)
            });
        }

        private Event ResolveMessage(ResolveFieldContext context)
        {
            return context.Source as Event;
        }

        private IObservable<Event> SubscribeAll(ResolveEventStreamContext context)
        {
            return observables.EventStream.AsObservable();
        }
    }
}
