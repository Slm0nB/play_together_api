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
                Resolver = new FuncFieldResolver<Event>(context => context.Source as Event),
                Subscriber = new EventStreamResolver<Event>(context => observables.EventStream.AsObservable())
            });

            AddField(new EventStreamFieldType
            {
                Name = "changedEventSignups",
                Type = typeof(UserEventSignupType),
                Arguments = new QueryArguments(
                    new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "event", Description = "The ID of the event." }
                ),
                Resolver = new FuncFieldResolver<UserEventSignup>(context => context.Source as UserEventSignup),
                Subscriber = new EventStreamResolver<UserEventSignup>(context =>
                {
                    var eventId = context.GetArgument<Guid>("event");
                    return observables.EventSignupStream
                        .Where(n => n.EventId == eventId)
                        .AsObservable();
                })
            });
        }
    }
}
