﻿using System;
using System.Linq;
using GraphQL.Types;
using GameCalendarApi.Domain;

namespace GameCalendarApi.Web.GraphQl
{
    public class UserType : ObjectGraphType<User>
    {
        public UserType()
        {
            Field("id", x => x.UserId, type: typeof(IdGraphType)).Description("Id property from the user object.");
            Field(x => x.DisplayName).Description("DisplayName property from the user object.");
        }
    }

    public class EventType : ObjectGraphType<Event>
    {
        public EventType(GameCalendarDbContext db)
        {
            Field("id", x => x.EventId, type: typeof(IdGraphType)).Description("Id property from the event object.");
            Field(x => x.CreatedDate, type: typeof(IdGraphType)).Description("CreatedDate property from the event object.");
            Field(x => x.Title).Description("Title property from the event object.");

            Field<ListGraphType<UserType>>("author",
                resolve: x => db.Users.Where(n => n.UserId == x.Source.CreatedByUserId)
            );
        }
    }

    public class GameCalendarQuery : ObjectGraphType
    {
        public GameCalendarQuery(GameCalendarDbContext db)
        {
            Name = "Query";

            Field<ListGraphType<EventType>>(
               "events",
                arguments: new QueryArguments(
                   new QueryArgument<StringGraphType> { Name = "id", Description = "Id of the event" },
                   new QueryArgument<IntGraphType> { Name = "skip", Description = "How many events to skip" },
                   new QueryArgument<IntGraphType> { Name = "take", Description = "How many events to return" }
                ),
               resolve: context =>
               {
                   IQueryable<Event> query = db.Events;

                   var id = context.GetArgument<string>("id");
                   if (Guid.TryParse(id, out var uid))
                   {
                       query = query.Where(n => n.EventId == uid);
                   }

                   var skip = context.GetArgument<int>("skip");
                   if (skip > 0)
                   {
                       query = query.Skip(skip);
                   }

                   var take = context.GetArgument<int>("take");
                   if (take > 0)
                   {
                       query = query.Take(take);
                   }

                   return query;
               }
           );

            Field<ListGraphType<UserType>>(
               "users",
               arguments: new QueryArguments(
                   new QueryArgument<StringGraphType> { Name = "id", Description = "Id of the user" },
                   new QueryArgument<IntGraphType> { Name = "skip", Description = "How many users to skip" },
                   new QueryArgument<IntGraphType> { Name = "take", Description = "How many users to return" }
                ),
               resolve: context =>
               {
                   IQueryable<User> query = db.Users;

                   var id = context.GetArgument<string>("id");
                   if (Guid.TryParse(id, out var uid))
                   {
                       query = query.Where(n => n.UserId == uid);
                   }

                   var skip = context.GetArgument<int>("skip");
                   if (skip > 0)
                   {
                       query = query.Skip(skip);
                   }

                   var take = context.GetArgument<int>("take");
                   if (take > 0)
                   {
                       query = query.Take(take);
                   }

                   return query;
               }
           );

            Field<UserType>(
               "me",
               resolve: context => db.Users.FirstOrDefault() // todo: check the claim on the user context and return the correct user, or null
           );
        }
    }
}
