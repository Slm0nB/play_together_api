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

            FieldAsync<EventType>(
                "createEvent",
                description: "Create a new event. This requires the caller to be authorized.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "title" }
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
                        EventId = Guid.NewGuid(),
                        Title = context.GetArgument<string>("title"),
                        CreatedDate = DateTime.Now,
                        CreatedByUserId = userId
                    };
                    db.Events.Add(newEvent);
                    await db.SaveChangesAsync();

                    return newEvent;
                }
            );

            FieldAsync<UserType>(
                "createUser",
                description: "Create a new user. This will fail if the emali is already in use.",
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

            FieldAsync<TokenResponseType>(
                "authenticate",
                description: "Request an access- and refresh-token for a user.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "email" },
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "password" }
                ),
                resolve: async context =>
                {
                    var requestDto = new TokenRequestModel
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
