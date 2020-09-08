using System;
using Microsoft.Extensions.DependencyInjection;
using GraphQL;
using GraphQL.Types;
using PlayTogetherApi.Services;
using PlayTogetherApi.Data;
using PlayTogetherApi.Models;
using PlayTogetherApi.Web.Models;
using PlayTogetherApi.Web.GraphQl.Types;

namespace PlayTogetherApi.Web.GraphQl
{
    public class PlayTogetherMutation : ObjectGraphType
    {
        public PlayTogetherMutation()
        {
            Name = "Mutation";

            FieldAsync<BooleanGraphType>( // todo: this should return an object instead
                "joinEvent",
                description: "Add the currently logged in user to an event.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "event" },
                    new QueryArgument<UserEventStatusGraphType> { Name = "status", DefaultValue = UserEventStatus.AcceptedInvitation }
                ),
                resolve: async context =>
                {
                    try
                    {
                        var interactionsService = context.RequestServices.GetService<InteractionsService>();

                        var claimedUserId = context.GetClaimedUserId();
                        var eventId = context.GetArgument<Guid>("event");
                        var status = context.GetArgument<UserEventStatus>("status");

                        var newSignup = await interactionsService.JoinEventAsync(claimedUserId, eventId, status);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        context.Errors.Add(new ExecutionError(ex.Message));
                        return false;
                    }
                }
            );

            FieldAsync<BooleanGraphType>( // todo: this should return an object instead
                "leaveEvent",
                description: "Remove the currently logged in user from an event.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "event" }
                ),
                resolve: async context =>
                {
                    try
                    {
                        var interactionsService = context.RequestServices.GetService<InteractionsService>();

                        var claimedUserId = context.GetClaimedUserId();
                        var eventId = context.GetArgument<Guid>("event");

                        var newSignup = await interactionsService.LeaveEventAsync(claimedUserId, eventId);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        context.Errors.Add(new ExecutionError(ex.Message));
                        return false;
                    }
                }
            );

            FieldAsync<UserEventSignupGraphType>(
                "updateSignup",
                description: "Update the current or any users signup state for an event. This requires the caller to be authorized.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "event", Description = "Id of the event to update the status for." },
                    new QueryArgument<NonNullGraphType<UserEventStatusGraphType>> { Name = "status", Description = "The new status." },
                    new QueryArgument<IdGraphType> { Name = "user", Description = "Id of the user, for event-owners changing the status of signsup to their event." }
                ),
                resolve: async context =>
                {
                    try
                    {
                        var interactionsService = context.RequestServices.GetService<InteractionsService>();

                        var claimedUserId = context.GetClaimedUserId();
                        var eventId = context.GetArgument<Guid>("event");
                        var status = context.GetArgument<UserEventStatus>("status"); ;
                        var userId = context.GetArgument<Guid>("user", claimedUserId);

                        var newSignup = await interactionsService.UpdateSignupAsync(userId, eventId, status, claimedUserId);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        context.Errors.Add(new ExecutionError(ex.Message));
                        return false;
                    }
                }
            );

            FieldAsync<EventGraphType>(
                "callToArms",
                description: "Create a new call-to-arms event. This requires the caller to be authorized.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<DateTimeGraphType>> { Name = "startdate" },
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "title" },
                    new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "game" }
                ),
                resolve: async context =>
                {
                    try
                    {
                        var interactionsService = context.RequestServices.GetService<InteractionsService>();

                        var claimedUserId = context.GetClaimedUserId();
                        var startDate = context.GetArgument<DateTime>("startdate");
                        var gameId = context.GetArgument<Guid>("game");
                        var title = context.GetArgument<string>("title");

                        var newEvent = await interactionsService.CallToArmsAsync(claimedUserId, startDate, title, gameId);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        context.Errors.Add(new ExecutionError(ex.Message));
                        return false;
                    }
                }
            );

            FieldAsync<EventGraphType>(
                "createEvent",
                description: "Create a new event. This requires the caller to be authorized.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<DateTimeGraphType>> { Name = "startdate" },
                    new QueryArgument<NonNullGraphType<DateTimeGraphType>> { Name = "enddate" },
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "title" },
                    new QueryArgument<StringGraphType> { Name = "description" },
                    new QueryArgument<BooleanGraphType> { Name = "friendsOnly", DefaultValue = false },
                    new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "game" }
                ),
                resolve: async context =>
                {
                    try
                    {
                        var interactionsService = context.RequestServices.GetService<InteractionsService>();

                        var claimedUserId = context.GetClaimedUserId();
                        var startDate = context.GetArgument<DateTime>("startdate");
                        var endDate = context.GetArgument<DateTime>("enddate");
                        var title = context.GetArgument<string>("title");
                        var description = context.GetArgument<string>("description");
                        var friendsOnly = context.HasArgument("friendsOnly") ? context.GetArgument<bool>("friendsOnly") : false;
                        var gameId = context.GetArgument<Guid>("game");

                        var newEvent = await interactionsService.CreateEventAsync(claimedUserId, startDate, endDate, title, description, friendsOnly, gameId);
                        return newEvent;
                    }
                    catch(Exception ex)
                    {
                        context.Errors.Add(new ExecutionError(ex.Message));
                        return null;
                    }
                }
            );

            FieldAsync<EventGraphType>(
                "updateEvent",
                description: "Update an event. This requires the caller to be authorized, and be the creator of the event.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "id", Description = "The ID of the event." },
                    new QueryArgument<DateTimeGraphType> { Name = "startdate" },
                    new QueryArgument<DateTimeGraphType> { Name = "enddate" },
                    new QueryArgument<StringGraphType> { Name = "title" },
                    new QueryArgument<StringGraphType> { Name = "description" },
                    new QueryArgument<BooleanGraphType> { Name = "friendsOnly" },
                    new QueryArgument<IdGraphType> { Name = "game" }
                ),
                resolve: async context =>
                {
                    try
                    {
                        var interactionsService = context.RequestServices.GetService<InteractionsService>();

                        var claimedUserId = context.GetClaimedUserId();
                        var eventId = context.GetArgument<Guid>("id");
                        var startDate = context.HasArgument("startdate") ? (DateTime?)context.GetArgument<DateTime>("startdate") : null;
                        var endDate = context.HasArgument("enddate") ? (DateTime?)context.GetArgument<DateTime>("enddate") : null;
                        var title = context.GetArgument<string>("title");
                        var description = context.GetArgument<string>("description");
                        var friendsOnly = context.HasArgument("friendsOnly") ? (bool?)context.GetArgument<bool>("friendsOnly") : null;
                        var gameId = context.HasArgument("game") ? (Guid?)context.GetArgument<Guid>("game") : null;

                        var newEvent = await interactionsService.UpdateEventAsync(claimedUserId, eventId, startDate, endDate, title, description, friendsOnly, gameId);
                        return newEvent;
                    }
                    catch (Exception ex)
                    {
                        context.Errors.Add(new ExecutionError(ex.Message));
                        return null;
                    }
                }
            );

            FieldAsync<BooleanGraphType>( // todo: this should return an object instead
                "deleteEvent",
                description: "Delete an event.  This can only be done by the creator of the event, and requires the caller to be authorized.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "id", Description = "The ID of the event." }
                ),
                resolve: async context =>
                {
                    try
                    {
                        var interactionsService = context.RequestServices.GetService<InteractionsService>();

                        var claimedUserId = context.GetClaimedUserId();
                        var eventId = context.GetArgument<Guid>("id");

                        var deletedEvent  = await interactionsService.DeleteEventAsync(claimedUserId, eventId);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        context.Errors.Add(new ExecutionError(ex.Message));
                        return null;
                    }
                }
            );

            FieldAsync<UserGraphType>(
                "createUser",
                description: "Create a new user. This will fail if the email is already in use.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "displayName" },
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "email" },
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "password" },
                    new QueryArgument<IntGraphType> { Name = "utcOffset", Description = "UTC offset in seconds" },
                    new QueryArgument<StringGraphType> { Name = "deviceToken", Description = "FCM device token" }
                ),
                resolve: async context =>
                {
                    try
                    {
                        var interactionsService = context.RequestServices.GetService<InteractionsService>();

                        var displayName = context.GetArgument<string>("displayName");
                        var email = context.GetArgument<string>("email");
                        var password = context.GetArgument<string>("password");
                        var utcOffset = context.HasArgument("utcOffset") ? TimeSpan.FromSeconds(context.GetArgument<int>("utcOffset")) : TimeSpan.Zero;
                        var deviceToken = context.HasArgument("deviceToken") ? context.GetArgument<string>("deviceToken") : null;

                        var newUser = await interactionsService.CreateUserAsync(displayName, email, password, utcOffset, deviceToken);
                        return newUser;
                    }
                    catch (Exception ex)
                    {
                        context.Errors.Add(new ExecutionError(ex.Message));
                        return null;
                    }
                }
            );

            FieldAsync<UserGraphType>(
                "updateUser",
                description: "Update the currently logged in user.",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "displayName" },
                    new QueryArgument<StringGraphType> { Name = "email" },
                    new QueryArgument<StringGraphType> { Name = "password" },
                    new QueryArgument<StringGraphType> { Name = "avatar", Description = "Filename of the new avatar image" },
                    new QueryArgument<IntGraphType> { Name = "utcOffset", Description = "UTC offset in seconds" },
                    new QueryArgument<StringGraphType> { Name = "deviceToken", Description = "FCM device token" }
                ),
                resolve: async context =>
                {
                    try
                    {
                        var interactionsService = context.RequestServices.GetService<InteractionsService>();

                        var claimedUserId = context.GetClaimedUserId();
                        var displayName = context.GetArgument<string>("displayName");
                        var email = context.GetArgument<string>("email");
                        var password = context.GetArgument<string>("password");
                        var avatar = context.GetArgument<string>("avatar");
                        var utcOffset = context.HasArgument("utcOffset") ? (TimeSpan?)TimeSpan.FromSeconds(context.GetArgument<int>("utcOffset")) : null;
                        var deviceToken = context.GetArgument<string>("deviceToken");

                        var user = await interactionsService.ModifyUserAsync(claimedUserId, displayName, email, password, avatar, utcOffset, deviceToken);
                        return user;
                    }
                    catch (Exception ex)
                    {
                        context.Errors.Add(new ExecutionError(ex.Message));
                        return null;
                    }
                }
            );

            FieldAsync<BooleanGraphType>( // todo: return object
                "deleteUser",
                description: "Delete the logged-in user. This requires the caller to be authorized. (STILL A WORK IN PROGRESS)",
                resolve: async context =>
                {
                    try
                    {
                        var interactionsService = context.RequestServices.GetService<InteractionsService>();

                        var userId = Extensions.GetClaimedUserId(context);

                        await interactionsService.DeleteUserAsync(userId);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        context.Errors.Add(new ExecutionError(ex.Message));
                        return null;
                    }
                }
            );

            FieldAsync<UserRelationGraphType>(
                "changeUserRelation",
                description: "Invite a user to your friendlist, or accept an invitation from a user. This requires the caller to be authorized.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "user" },
                    new QueryArgument<NonNullGraphType<UserRelationActionGraphType>> { Name = "status" }
                ),
                resolve: async context =>
                {
                    try
                    {
                        var interactionsService = context.RequestServices.GetService<InteractionsService>();

                        var claimedUserId = context.GetClaimedUserId();
                        var friendUserId = context.GetArgument<Guid>("user");
                        var action = context.GetArgument<UserRelationAction>("status");

                        var result = await interactionsService.ChangeUserRelationAsync(claimedUserId, friendUserId, action);
                        return result;
                    }
                    catch (Exception ex)
                    {
                        context.Errors.Add(new ExecutionError(ex.Message));
                        return null;
                    }
                }
            );

            FieldAsync<TokenResponseGraphType>(
                "token",
                description: "Request authorization tokens for a user, based on email/password or a refresh token.",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "email" },
                    new QueryArgument<StringGraphType> { Name = "password" },
                    new QueryArgument<StringGraphType> { Name = "refreshToken" },
                    new QueryArgument<IntGraphType> { Name = "accessTokenLifetime", Description = "Optional lifetime in minutes. Max: 90." },
                    new QueryArgument<IntGraphType> { Name = "refreshTokenLifetime", Description = "Optional lifetime in minutes. Max: 43200 (30 days)." }
                ),
                resolve: async context =>
                {
                    var authenticationService = context.RequestServices.GetService<AuthenticationService>();

                    var requestDto = context.HasArgument("refreshToken")
                        ? new TokenRequestModel
                            {
                                Grant_type = "refresh_token",
                                Refresh_token = context.GetArgument<Guid>("refreshToken")
                            }
                        : new TokenRequestModel
                            {
                                Grant_type = "password",
                                Username = context.GetArgument<string>("email"),
                                Password = context.GetArgument<string>("password")
                            };

                    if(context.HasArgument("accessTokenLifetime"))
                    {
                        requestDto.AccessTokenLifetime = context.GetArgument<int>("accessTokenLifetime");
                    }
                    if (context.HasArgument("refreshTokenLifetime"))
                    {
                        requestDto.RefreshTokenLifetime = context.GetArgument<int>("refreshTokenLifetime");
                    }

                    var response = await authenticationService.RequestTokenAsync(requestDto);

                    return response;
                }
            );
        }
    }
}
