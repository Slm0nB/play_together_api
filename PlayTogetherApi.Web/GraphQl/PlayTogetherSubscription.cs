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
        ObservablesService observables;

        public PlayTogetherSubscription(PlayTogetherDbContext db, AuthenticationService authenticationService, ObservablesService observables)
        {
            this.observables = observables;

            Name = "Subscription";

            AddField(new EventStreamFieldType
            {
                // todo: add arguments for filtering
                Name = "events",
                Description = "Created or updated events.",
                Type = typeof(EventBaseType),
                Resolver = new FuncFieldResolver<Event>(context => context.Source as Event),
                Subscriber = new EventStreamResolver<Event>(context => observables.GameEventStream.AsObservable())
            });

            AddField(new EventStreamFieldType
            {
                Name = "signups",
                Description = "Users joining an event or updating their signup-status.",
                Type = typeof(UserEventSignupType),
                Arguments = new QueryArguments(
                    new QueryArgument<IdGraphType> { Name = "owner", Description = "The ID of the user who created the event." },
                    new QueryArgument<IdGraphType> { Name = "user", Description = "The ID of the user joining or leaving the event." },
                    new QueryArgument<IdGraphType> { Name = "event", Description = "The ID of the event." }
                ),
                Resolver = new FuncFieldResolver<UserEventSignup>(context => context.Source as UserEventSignup),
                Subscriber = new EventStreamResolver<UserEventSignup>(context =>
                {
                    var observable = observables.UserEventSignupStream;

                    if(context.HasArgument("event"))
                    {
                        var eventId = context.GetArgument<Guid>("event");
                        observable = (ISubject<UserEventSignup>)observable.Where(n => n.EventId == eventId);
                    }

                    if (context.HasArgument("user"))
                    {
                        var userId = context.GetArgument<Guid>("user");
                        observable = (ISubject<UserEventSignup>)observable.Where(n => n.UserId == userId);
                    }

                    if (context.HasArgument("owner"))
                    {
                        var ownerId = context.GetArgument<Guid>("owner");
                        observable = (ISubject<UserEventSignup>)observable.Where(n => n.Event.CreatedByUserId == ownerId);
                    }

                    return observable.AsObservable();
                })
            });


            AddField(new EventStreamFieldType
            {
                Name = "friends",
                Description = "Changes to the friendlist",
                Type = typeof(UserRelationChangeType),
                Arguments = new QueryArguments(
                    new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "token", Description = "Access-token. Because it currently can't be provided as a header for subscriptions." },
                    new QueryArgument<BooleanGraphType> { Name = "excludeChangesFromCaller", Description = "Don't return changes that were triggered by the calling user.", DefaultValue = true }
                ),
                Resolver = new FuncFieldResolver<UserRelationExtModel>(context => context.Source as UserRelationExtModel),
                Subscriber = new EventStreamResolver<UserRelationExtModel>(context =>
                {
                    var principal = context.UserContext as ClaimsPrincipal;
                    var userIdClaim = principal?.Claims.FirstOrDefault(n => n.Type == "userid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var callingUserId))
                    {
                        context.Errors.Add(new ExecutionError("Unauthorized"));
                        return null;
                    }

                    IObservable<UserRelationExtModel> observable = observables.UserRelationStream
                        .Where(rel => rel.Relation.UserAId == callingUserId || rel.Relation.UserBId == callingUserId);

                    if (!context.GetArgument<bool>("excludeChangesFromCaller"))
                    {
                        observable = observable.Where(rel => rel.PrimaryUserId != callingUserId);
                    }

                    return observable.AsObservable();
                })
            });
        }
    }
}
