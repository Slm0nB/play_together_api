using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using GraphQL;
using GraphQL.Types;
using GraphQL.Subscription;
using GraphQL.Resolvers;
using PlayTogetherApi.Services;
using PlayTogetherApi.Data;
using PlayTogetherApi.Web.Models;
using PlayTogetherApi.Web.GraphQl.Types;
using PlayTogetherApi.Web.Services;

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
                Name = "timestamp",
                Description = "A simple test-eventstream sending a timestamp every few seconds.",
                Arguments = new QueryArguments(
                    new QueryArgument<IntGraphType> { Name = "interval", Description = "Interval in seconds.", DefaultValue = 5 }
                ),
                Type = typeof(TimestampGraphType),
                Resolver = new FuncFieldResolver<TimestampModel>(context => context.Source as TimestampModel),
                Subscriber = new EventStreamResolver<TimestampModel>(context =>
                    Observable
                        .Interval(TimeSpan.FromSeconds(context.GetArgument<int>("interval", 5)))
                        .StartWith(0)
                        .Select(n => new TimestampModel
                        {
                            DateTime = DateTime.Now
                        })
                )
            });

            AddField(new EventStreamFieldType
            {
                // todo: add arguments for filtering based on action
                Name = "events",
                Description = "Created, updated or deleted events. (The events themselves, not signups.)",
                Type = typeof(EventChangedGraphType),
                Arguments = new QueryArguments(
                    new QueryArgument<IdGraphType> { Name = "token", Description = "Access-token. Because it currently can't be provided as a header for subscriptions. This is required to get updates for friendsOnly events." }
                ),
                Resolver = new FuncFieldResolver<EventChangedModel>(context => context.Source as EventChangedModel),
                Subscriber = new EventStreamResolver<EventChangedModel>(context =>
                {
                    Guid? userId = null;
                    if (context.HasArgument("token"))
                    {
                        var jwt = authenticationService.ValidateJwt(context.GetArgument<string>("token"));
                        var userIdClaim = jwt?.Claims.FirstOrDefault(n => n.Type == "userid")?.Value;
                        if (Guid.TryParse(userIdClaim, out var _userId))
                        {
                            userId = _userId;
                        }
                    }

                    return observables.GameEventStream
                        .Where(eventExt => !eventExt.Event.FriendsOnly || eventExt.Event.CreatedByUserId == userId || (userId.HasValue && eventExt.FriendsOfChangingUser != null && eventExt.FriendsOfChangingUser.Any(nn => nn.UserAId == userId ||nn.UserBId == userId)))
                        .AsObservable();
                })
            });

            AddField(new EventStreamFieldType
            {
                Name = "signups",
                Description = "Users joining an event or updating their signup-status.",
                Type = typeof(EventSignupChangedGraphType),
                Arguments = new QueryArguments(
                    new QueryArgument<IdGraphType> { Name = "owner", Description = "The ID of the user who created the event." },
                    new QueryArgument<IdGraphType> { Name = "user", Description = "The ID of the user joining or leaving the event." },
                    new QueryArgument<IdGraphType> { Name = "event", Description = "The ID of the event." }
                    // todo: "participant" parameter, so the user can get changes to all events they participate in ... which means also adding the "token" parameter
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
                Description = "Changes to the friendlist. This only returns changed relevant to the authenticated user.",
                Type = typeof(UserRelationChangedGraphType),
                Arguments = new QueryArguments(
                    new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "token", Description = "Access-token. Because it currently can't be provided as a header for subscriptions." },
                    new QueryArgument<BooleanGraphType> { Name = "excludeChangesFromCaller", Description = "Don't return changes that were triggered by the subscribing user.", DefaultValue = true }
                ),
                Resolver = new FuncFieldResolver<UserRelationChangedExtModel>(context => context.Source as UserRelationChangedExtModel),
                Subscriber = new EventStreamResolver<UserRelationChangedExtModel>(context =>
                {
                    var jwt = authenticationService.ValidateJwt(context.GetArgument<string>("token"));
                    var userIdClaim = jwt?.Claims.FirstOrDefault(n => n.Type == "userid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var callingUserId))
                    {
                        context.Errors.Add(new ExecutionError("Unauthorized"));
                        return null;
                    }

                    IObservable<UserRelationChangedModel> observable = observables.UserRelationChangeStream
                        .Where(rel => rel.Relation.UserAId == callingUserId || rel.Relation.UserBId == callingUserId);

                    if (context.GetArgument<bool>("excludeChangesFromCaller"))
                    {
                        observable = observable.Where(rel => rel.ActiveUser.UserId != callingUserId);
                    }

                    return observable
                        .Select(n => new UserRelationChangedExtModel(n, callingUserId))
                        .AsObservable();
                })
            });

            AddField(new EventStreamFieldType
            {
                Name = "users",
                Description = "Changes to users. This only returns changed relevant to the authenticated user.",
                Type = typeof(UserChangedGraphType),
                Arguments = new QueryArguments(
                    new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "token", Description = "Access-token. Because it currently can't be provided as a header for subscriptions." },
                    new QueryArgument<BooleanGraphType> { Name = "excludeChangesFromCaller", Description = "Don't return changes that were triggered by the subscribing user.", DefaultValue = true }
                ),
                Resolver = new FuncFieldResolver<UserChangedSubscriptionModel>(context => context.Source as UserChangedSubscriptionModel),
                Subscriber = new EventStreamResolver<UserChangedSubscriptionModel>(context =>
                {
                    var jwt = authenticationService.ValidateJwt(context.GetArgument<string>("token"));
                    var userIdClaim = jwt?.Claims.FirstOrDefault(n => n.Type == "userid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var callingUserId))
                    {
                        context.Errors.Add(new ExecutionError("Unauthorized"));
                        return null;
                    }

                    IObservable<UserChangedSubscriptionModel> observable = observables.UserChangeStream
                        .Where(model => model.FriendsOfChangingUser.Any(rel => rel.UserAId == callingUserId || rel.UserBId == callingUserId));

                    if (context.GetArgument<bool>("excludeChangesFromCaller"))
                    {
                        observable = observable.Where(rel => rel.ChangingUser.UserId != callingUserId);
                    }

                    return observable
                        .AsObservable();
                })
            });

            AddField(new EventStreamFieldType
            {
                Name = "userStatistics",
                Description = "Changes to the user statistics. This only returns changed relevant to the authenticated user.",
                Type = typeof(UserStatisticsGraphType),
                Arguments = new QueryArguments(
                    new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "token", Description = "Access-token. Because it currently can't be provided as a header for subscriptions." }
                ),
                Resolver = new FuncFieldResolver<UserStatisticsModel>(context => context.Source as UserStatisticsModel),
                Subscriber = new EventStreamResolver<UserStatisticsModel>(context =>
                {
                    var jwt = authenticationService.ValidateJwt(context.GetArgument<string>("token"));
                    var userIdClaim = jwt?.Claims.FirstOrDefault(n => n.Type == "userid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var callingUserId))
                    {
                        context.Errors.Add(new ExecutionError("Unauthorized"));
                        return null;
                    }

                    IObservable<UserStatisticsModel> observable = observables.GetUserStatisticsStream(callingUserId);

                    return observable;
                })
            });


            AddField(new EventStreamFieldType
            {
                Name = "eventSearch",
                Description = "Changes to a search for events.",
                Type = typeof(EventSearchUpdateGraphType),
                Arguments = new QueryArguments(
                   new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "token", Description = "Access-token. Because it currently can't be provided as a header for subscriptions." },

                   new QueryArgument<StringGraphType> { Name = "search", Description = "Search term applied to the title or description." },
                   new QueryArgument<DateTimeGraphType> { Name = "startsBeforeDate", Description = "Event starts before or on this datetime." },
                   new QueryArgument<DateTimeGraphType> { Name = "startsAfterDate", Description = "Event starts on or after this datetime." },
                   new QueryArgument<DateTimeGraphType> { Name = "endsBeforeDate", Description = "Event ends before or on this datetime." },
                   new QueryArgument<DateTimeGraphType> { Name = "endsAfterDate", Description = "Event ends on or after this datetime. If no start/end arguments are given, this default to 'now'." },

                   new QueryArgument<BooleanGraphType> { Name = "onlyPrivate", Description = "Only show events that are friends-only." },
                   new QueryArgument<BooleanGraphType> { Name = "onlyByFriends", Description = "Only show events that are created by friends. This requires the caller to be authorized." },
                   new QueryArgument<ListGraphType<NonNullGraphType<IdGraphType>>> { Name = "onlyByUsers", Description = "Only show events created by these users." },
                   new QueryArgument<ListGraphType<NonNullGraphType<IdGraphType>>> { Name = "onlyGames", Description = "Only show events for these games." },
                   new QueryArgument<BooleanGraphType> { Name = "onlyJoined", Description = "Only show events that are joined by the user. This requires the caller to be authorized." },

                   new QueryArgument<BooleanGraphType> { Name = "includePrivate", Description = "Include events that are friends-only." },
                   new QueryArgument<BooleanGraphType> { Name = "includeByFriends", Description = "Include events that are created by friends. This requires the caller to be authorized." },
                   new QueryArgument<ListGraphType<NonNullGraphType<IdGraphType>>> { Name = "includeByUsers", Description = "Include events created by these users." },
                   new QueryArgument<ListGraphType<NonNullGraphType<IdGraphType>>> { Name = "includeGames", Description = "Include events for these games." },
                   new QueryArgument<BooleanGraphType> { Name = "includeJoined", Description = "Include events that are joined by the user. This requires the caller to be authorized." }
                ),
                Resolver = new FuncFieldResolver<EventSearchUpdateModel>(context => context.Source as EventSearchUpdateModel),
                Subscriber = new EventStreamResolver<EventSearchUpdateModel>(context =>
                {
                    var jwt = authenticationService.ValidateJwt(context.GetArgument<string>("token"));
                    var userIdClaim = jwt?.Claims.FirstOrDefault(n => n.Type == "userid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var callingUserId))
                    {
                        context.Errors.Add(new ExecutionError("Unauthorized"));
                        return null;
                    }

                    var queryService = new EventsQueryService();

                    // todo: populate userid and friends etc, like in PlayTogetherQuery

                    queryService.ReadParametersFromContext(context);
                    var initialEvents = queryService.Process(db.Events).ToList();  // todo: look into turning it async!

                    var observable = new EventSearchObservable(observables, queryService, initialEvents);

                    return observable;
                })
            });
        }
    }
}
