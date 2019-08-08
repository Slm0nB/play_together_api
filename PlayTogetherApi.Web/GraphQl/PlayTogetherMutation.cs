using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using GraphQL.Types;
using PlayTogetherApi.Services;
using PlayTogetherApi.Domain;
using PlayTogetherApi.Web.Models;
using PlayTogetherApi.Web.GraphQl.Types;

namespace PlayTogetherApi.Web.GraphQl
{
    public class PlayTogetherMutation : ObjectGraphType
    {
        public PlayTogetherMutation(PlayTogetherDbContext db, AuthenticationService authenticationService)
        {
            Name = "Mutation";

            FieldAsync<BooleanGraphType>(
                "joinEvent",
                description: "Add the currently logged in user to an event.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "event" }
                ),
                resolve: async context =>
                {
                    var principal = context.UserContext as ClaimsPrincipal;
                    var userIdClaim = principal.Claims.FirstOrDefault(n => n.Type == "userid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var userId))
                        return false;
                    if (!await db.Users.AnyAsync(n => n.UserId == userId))
                        return false;

                    if (!context.HasArgument("event"))
                        return false;
                    var eventId = context.GetArgument<Guid>("event");
                    if (!await db.Events.AnyAsync(n => n.EventId == eventId))
                        return false;

                    var signup = new UserEventSignup {
                        EventId = eventId,
                        UserId = userId
                    };
                    db.UserEventSignups.Add(signup);
                    await db.SaveChangesAsync();

                    return true;
                }
            );

            FieldAsync<BooleanGraphType>(
                "leaveEvent",
                description: "Remove the currently logged in user from an event.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "event" }
                ),
                resolve: async context =>
                {
                    var principal = context.UserContext as ClaimsPrincipal;
                    var userIdClaim = principal.Claims.FirstOrDefault(n => n.Type == "userid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var userId))
                        return false;
                    if (!await db.Users.AnyAsync(n => n.UserId == userId))
                        return false;

                    if (!context.HasArgument("event"))
                        return false;
                    var eventId = context.GetArgument<Guid>("event");

                    var signup = await db.UserEventSignups.FirstOrDefaultAsync(n => n.EventId == eventId && n.UserId == userId);
                    if (signup == null)
                        return false;

                    db.UserEventSignups.Remove(signup);
                    await db.SaveChangesAsync();

                    return true;
                }
            );

            FieldAsync<EventType>(
                "createEvent",
                description: "Create a new event. This requires the caller to be authorized.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<DateGraphType>> { Name = "date" },
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "title" },
                    new QueryArgument<StringGraphType> { Name = "description" },
                    new QueryArgument<IdGraphType> { Name = "game" }
                ),
                resolve: async context =>
                {
                    var principal = context.UserContext as ClaimsPrincipal;
                    var userIdClaim = principal.Claims.FirstOrDefault(n => n.Type == "userid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var userId))
                        return null;

                    if(!await db.Users.AnyAsync(n => n.UserId == userId))
                        return null;

                    var newEvent = new Event
                    {
                        Title = context.GetArgument<string>("title"),
                        CreatedByUserId = userId,
                        EventDate = context.GetArgument<DateTime>("date"),
                        Description = context.GetArgument<string>("description"),
                        GameId = context.GetArgument<Guid?>("game")
                    };
                    db.Events.Add(newEvent);
                    await db.SaveChangesAsync();

                    return newEvent;
                }
            );

            FieldAsync<EventType>(
                "updateEvent",
                description: "Update an event. This requires the caller to be authorized, and be the creator of the event.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "id", Description = "The ID of the event." },
                    new QueryArgument<DateGraphType> { Name = "date" },
                    new QueryArgument<StringGraphType> { Name = "title" },
                    new QueryArgument<StringGraphType> { Name = "description" },
                    new QueryArgument<IdGraphType> { Name = "game" }
                ),
                resolve: async context =>
                {
                    var principal = context.UserContext as ClaimsPrincipal;
                    var userIdClaim = principal.Claims.FirstOrDefault(n => n.Type == "userid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var userId))
                        return null;

                    if (!await db.Users.AnyAsync(n => n.UserId == userId))
                        return null;

                    var eventId = context.GetArgument<Guid>("id");
                    var editedEvent = await db.Events.FirstOrDefaultAsync(n => n.EventId == eventId);
                    if (editedEvent == null || editedEvent.CreatedByUserId != userId)
                        return null;

                    if (context.HasArgument("date"))
                    {
                        var date = context.GetArgument<DateTime>("date");
                        if (date != default(DateTime))
                        {
                            editedEvent.EventDate = date;
                        }
                    }

                    var title = context.GetArgument<string>("title");
                    if (!string.IsNullOrEmpty(title))
                    {
                        editedEvent.Title = title;
                    }

                    var description = context.GetArgument<string>("description");
                    if (!string.IsNullOrEmpty(description))
                    {
                        editedEvent.Description = description;
                    }

                    if (context.HasArgument("game"))
                    {
                        var gameId = context.GetArgument<Guid>("game");
                        if (gameId != default(Guid))
                        {
                            var gameExists = await db.Games.AnyAsync(n => n.GameId == gameId);
                            if (gameExists)
                                return null;

                            editedEvent.GameId = gameId;
                        }
                    }

                    db.Events.Update(editedEvent);
                    await db.SaveChangesAsync();

                    return editedEvent;
                }
            );

            FieldAsync<UserType>(
                "createUser",
                description: "Create a new user. This will fail if the email is already in use.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "displayName" },
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "email" },
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "password" }
                ),
                resolve: async context =>
                {
                    var displayName = context.GetArgument<string>("displayName");
                    var email = context.GetArgument<string>("email");
                    var password = context.GetArgument<string>("password");

                    // todo: validation

                    var userExists = await db.Users.AnyAsync(n => n.Email == email);
                    if (userExists)
                        return null;

                    var passwordHash = authenticationService.CreatePasswordHash(password);

                    var newUser = new User
                    {
                        DisplayName = displayName,
                        Email = email,
                        PasswordHash = passwordHash
                    };

                    db.Users.Add(newUser);
                    await db.SaveChangesAsync();

                    return newUser;
                }
            );

            FieldAsync<UserType>(
                "updateUser",
                description: "Update the currently logged in user.",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "displayName" },
                    new QueryArgument<StringGraphType> { Name = "email" },
                    new QueryArgument<StringGraphType> { Name = "password" }
                ),
                resolve: async context =>
                {
                    var principal = context.UserContext as ClaimsPrincipal;
                    var userIdClaim = principal.Claims.FirstOrDefault(n => n.Type == "userid")?.Value;
                    if (!Guid.TryParse(userIdClaim, out var userId))
                        return null;

                    var editedUser = await db.Users.FirstOrDefaultAsync(n => n.UserId == userId);
                    if(editedUser == null)
                        return null;

                    var displayName = context.GetArgument<string>("displayName");
                    if(!string.IsNullOrEmpty(displayName))
                    {
                        editedUser.DisplayName = displayName;
                    }

                    var email = context.GetArgument<string>("email");
                    if(!string.IsNullOrEmpty(email))
                    {
                        // todo: validation
                        editedUser.Email = email;
                    }

                    var password = context.GetArgument<string>("password");
                    if(!string.IsNullOrEmpty(password))
                    {
                        // todo: validation
                        editedUser.PasswordHash = authenticationService.CreatePasswordHash(password);
                    }

                    db.Users.Update(editedUser);
                    await db.SaveChangesAsync();

                    return editedUser;
                }
            );

            FieldAsync<TokenResponseType>(
                "token",
                description: "Request authorization tokens for a user, based on email/password or a refresh token.",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "email" },
                    new QueryArgument<StringGraphType> { Name = "password" },
                    new QueryArgument<StringGraphType> { Name = "refreshToken" }
                ),
                resolve: async context =>
                {
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

                    var response = await authenticationService.RequestTokenAsync(requestDto);

                    return response;
                }
            );
        }
    }
}
