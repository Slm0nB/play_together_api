using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using GraphQL.Types;
using GameCalendarApi.Services;
using GameCalendarApi.Domain;
using GameCalendarApi.Web.Models;
using GameCalendarApi.Web.GraphQl.Types;

namespace GameCalendarApi.Web.GraphQl
{
    public class GameCalendarMutation : ObjectGraphType
    {
        public GameCalendarMutation(GameCalendarDbContext db, AuthenticationService authenticationService)
        {
            Name = "Mutation";

            FieldAsync<EventType>(
                "createEvent",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<EventInputType>> { Name = "event" }
                ),
                resolve: async context =>
                {
                    var inputEvent = context.GetArgument<Event>("event");

                    // wtf deserialization doesnt work?
                    var tmp = (context.Arguments["event"] as IDictionary<string, object>)?["authorId"] as string;
                    var authorId = inputEvent.CreatedByUserId == Guid.Empty && !string.IsNullOrEmpty(tmp)
                        ? Guid.Parse(tmp)
                        : inputEvent.CreatedByUserId;

                    var newEvent = new Event
                    {
                        EventId = Guid.NewGuid(),
                        Title = inputEvent.Title,
                        CreatedDate = DateTime.Now,
                        CreatedByUserId = authorId
                    };
                    db.Events.Add(newEvent);
                    await db.SaveChangesAsync();

                    return newEvent;
                }
            );

            FieldAsync<UserType>(
                "createUser",
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
                        throw new ApplicationException();

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
