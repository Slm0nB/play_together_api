using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using GraphQL.Types;
using PlayTogetherApi.Domain;
using PlayTogetherApi.Web.GraphQl.Types;
using PlayTogetherApi.Web.Models;

namespace PlayTogetherApi.Web.GraphQl
{
    public class PlayTogetherQuery : ObjectGraphType
    {
        public PlayTogetherQuery(PlayTogetherDbContext db)
        {
            Name = "Query";

            Field<EventCollectionType>(
               "events",
                arguments: new QueryArguments(
                   new QueryArgument<StringGraphType> { Name = "id", Description = "Id of the event." },
                   new QueryArgument<StringGraphType> { Name = "search", Description = "Search term applied to the title or description." },
                   new QueryArgument<DateTimeGraphType> { Name = "beforeDate", Description = "Event occurs before or on this datetime." },
                   new QueryArgument<DateTimeGraphType> { Name = "afterDate", Description = "Event occurs on or after this datetime." },
                   new QueryArgument<IntGraphType> { Name = "skip", Description = "How many events to skip." },
                   new QueryArgument<IntGraphType> { Name = "take", Description = "How many events to return. Maximum 100.", DefaultValue = 100 }
                ),
               resolve: context =>
               {
                   IQueryable<Event> query = db.Events;

                   var id = context.GetArgument<string>("id");
                   if (Guid.TryParse(id, out var uid))
                   {
                       query = query.Where(n => n.EventId == uid);
                   }

                   var search = context.GetArgument<string>("search");
                   if (!string.IsNullOrWhiteSpace(search))
                   {
                       search = search.ToLowerInvariant();
                       query = query.Where(n => n.Title.ToLower().Contains(search) || n.Description.ToLower().Contains(search)); // todo: verify sql isnt retarded
                   }

                   var afterDate = context.GetArgument<DateTime>("afterDate");
                   if (afterDate != default(DateTime))
                   {
                       query = query.Where(n => n.EventDate >= afterDate);
                   }

                   var beforeDate = context.GetArgument<DateTime>("beforeDate");
                   if (beforeDate != default(DateTime))
                   {
                       query = query.Where(n => n.EventDate <= beforeDate);
                   }

                   var skip = context.GetArgument<int>("skip");
                   if (skip > 0)
                   {
                       query = query.Skip(skip);
                   }

                   var take = Math.Min(100, context.GetArgument<int>("take", 100));
                   if (take > 0)
                   {
                       query = query.Take(take);
                   }
                   else
                   {
                       return null;
                   }

                   return new EventCollectionModel
                   {
                       EventsQuery = query,
                       TotalEventsQuery = db.Events
                   };
               }
           );

            Field<UserCollectionType>(
               "users",
               arguments: new QueryArguments(
                   new QueryArgument<StringGraphType> { Name = "id", Description = "Id of the user." },
                   new QueryArgument<StringGraphType> { Name = "email", Description = "Email of the user." },
                   new QueryArgument<StringGraphType> { Name = "search", Description = "Search term." },
                   new QueryArgument<IntGraphType> { Name = "skip", Description = "How many users to skip." },
                   new QueryArgument<IntGraphType> { Name = "take", Description = "How many users to return. Maximum 100.", DefaultValue = 100 }
                ),
               resolve: context =>
               {
                   IQueryable<User> query = db.Users;

                   var id = context.GetArgument<string>("id");
                   if (Guid.TryParse(id, out var uid))
                   {
                       query = query.Where(n => n.UserId == uid);
                   }

                   var email = context.GetArgument<string>("email");
                   if (!string.IsNullOrWhiteSpace(email))
                   {
                       email = email.ToLowerInvariant();
                       query = query.Where(n => n.Email.ToLower().Contains(email)); // todo: verify sql isnt retarded
                   }

                   var search = context.GetArgument<string>("search");
                   if (!string.IsNullOrWhiteSpace(search))
                   {
                       search = search.ToLowerInvariant();
                       query = query.Where(n => n.DisplayName.ToLower().Contains(search) || n.Email.ToLower() == search); // todo: verify sql isnt retarded
                   }

                   var skip = context.GetArgument<int>("skip");
                   if (skip > 0)
                   {
                       query = query.Skip(skip);
                   }

                   var take = Math.Min(100, context.GetArgument<int>("take", 100));
                   if (take > 0)
                   {
                       query = query.Take(take);
                   }
                   else
                   {
                       return null;
                   }

                   return query;
               }
           );

            Field<GameCollectionType>(
               "games",
               arguments: new QueryArguments(
                   new QueryArgument<StringGraphType> { Name = "id", Description = "Id of the game." },
                   new QueryArgument<StringGraphType> { Name = "search", Description = "Search term applied to the title." },
                   new QueryArgument<IntGraphType> { Name = "skip", Description = "How many games to skip." },
                   new QueryArgument<IntGraphType> { Name = "take", Description = "How many games to return." }
                ),
               resolve: context =>
               {
                   IQueryable<Game> query = db.Games;

                   var id = context.GetArgument<string>("id");
                   if (Guid.TryParse(id, out var uid))
                   {
                       query = query.Where(n => n.GameId == uid);
                   }

                   var search = context.GetArgument<string>("search");
                   if (!string.IsNullOrWhiteSpace(search))
                   {
                       search = search.ToLowerInvariant();
                       query = query.Where(n => n.Title.ToLower().Contains(search)); // todo: verify sql isnt retarded
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

            Field<BuiltinAvatarCollectionType>(
               "avatars",
               arguments: new QueryArguments(
                   new QueryArgument<IntGraphType> { Name = "skip", Description = "How many games to skip." },
                   new QueryArgument<IntGraphType> { Name = "take", Description = "How many games to return." }
                ),
               resolve: context =>
               {
                   IQueryable<BuiltinAvatar> query = db.Avatars;

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

            FieldAsync<SelfUserType>(
               "me",
               description: "The details of the authorized user.",
               resolve: async context =>
               {
                   var principal = context.UserContext as ClaimsPrincipal;
                   var userIdClaim = principal.Claims.FirstOrDefault(n => n.Type == "userid")?.Value;
                   if (Guid.TryParse(userIdClaim, out var userId))
                   {
                       return await db.Users.FirstOrDefaultAsync(n => n.UserId == userId);
                   }
                   return null;
               }
           );
        }
    }
}
