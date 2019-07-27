﻿using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;
using GameCalendarApi.Domain;

namespace GameCalendarApi.Web.GraphQl
{
    public class GameCalendarMutation : ObjectGraphType
    {
        public class EventInputType : InputObjectGraphType<Event>
        {
            public EventInputType()
            {
                Name = "EventInput";
                Field(x => x.Title);
                Field<Guid>("authorId", x => x.CreatedByUserId, type: typeof(IdGraphType), nullable: false);
            }
        }

        public GameCalendarMutation(GameCalendarDbContext db, SecurityService securityService)
        {
            Name = "Mutation";

            Field<EventType>(
                "createEvent",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<EventInputType>> { Name = "event" }
                ),
                resolve: context =>
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
                    db.SaveChanges();

                    return newEvent;
                }
            );

            Field<UserType>(
                "createUser",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "displayName" },
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "email" },
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "password" }
                ),
                resolve: context =>
                {
                    var displayName = context.GetArgument<string>("displayName");
                    var email = context.GetArgument<string>("email");
                    var password = context.GetArgument<string>("password");

                    // todo: validation

                    var userExists = db.Users.Any(n => n.Email == email);
                    if (userExists)
                        throw new ApplicationException();

                    var passwordHash = securityService.CreatePasswordHash(password);

                    var newUser = new User
                    {
                        DisplayName = displayName,
                        Email = email,
                        PasswordHash = passwordHash
                    };

                    db.Users.Add(newUser);
                    db.SaveChanges();

                    return newUser;
                }
            );
        }
    }
}
