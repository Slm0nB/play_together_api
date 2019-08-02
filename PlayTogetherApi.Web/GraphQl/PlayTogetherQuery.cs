using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using GraphQL.Types;
using PlayTogetherApi.Domain;
using PlayTogetherApi.Web.GraphQl.Types;

namespace PlayTogetherApi.Web.GraphQl
{
    public class PlayTogetherQuery : ObjectGraphType
    {
        public PlayTogetherQuery(PlayTogetherDbContext db)
        {
            Name = "Query";

            FieldAsync<ListGraphType<EventType>>(
               "events",
                arguments: new QueryArguments(
                   new QueryArgument<StringGraphType> { Name = "id", Description = "Id of the event." },
                   new QueryArgument<StringGraphType> { Name = "search", Description = "Search term applied to the title or description." },
                   new QueryArgument<StringGraphType> { Name = "fromDate", Description = "Date before the event." },
                   new QueryArgument<StringGraphType> { Name = "toDate", Description = "Date after the event." },
                   new QueryArgument<IntGraphType> { Name = "skip", Description = "How many events to skip." },
                   new QueryArgument<IntGraphType> { Name = "take", Description = "How many events to return." }
                ),
               resolve: async context =>
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

                   var fromDate = context.GetArgument<DateTime>("fromDate");
                   if (fromDate != default(DateTime))
                   {
                       query = query.Where(n => n.EventDate >= fromDate);
                   }

                   var toDate = context.GetArgument<DateTime>("toDate");
                   if (toDate != default(DateTime))
                   {
                       query = query.Where(n => n.EventDate <= toDate);
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

                   return await query.ToListAsync();
               }
           );

            FieldAsync<ListGraphType<UserType>>(
               "users",
               arguments: new QueryArguments(
                   new QueryArgument<StringGraphType> { Name = "id", Description = "Id of the user." },
                   new QueryArgument<StringGraphType> { Name = "search", Description = "Search term applied to the displayname." },
                   new QueryArgument<IntGraphType> { Name = "skip", Description = "How many users to skip." },
                   new QueryArgument<IntGraphType> { Name = "take", Description = "How many users to return." }
                ),
               resolve: async context =>
               {
                   IQueryable<User> query = db.Users;

                   var id = context.GetArgument<string>("id");
                   if (Guid.TryParse(id, out var uid))
                   {
                       query = query.Where(n => n.UserId == uid);
                   }

                   var search = context.GetArgument<string>("search");
                   if (!string.IsNullOrWhiteSpace(search))
                   {
                       search = search.ToLowerInvariant();
                       query = query.Where(n => n.DisplayName.ToLower().Contains(search)); // todo: verify sql isnt retarded
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

                   return await query.ToListAsync();
               }
           );


            FieldAsync<ListGraphType<GameType>>(
               "games",
               arguments: new QueryArguments(
                   new QueryArgument<StringGraphType> { Name = "id", Description = "Id of the game." },
                   new QueryArgument<StringGraphType> { Name = "search", Description = "Search term applied to the title." },
                   new QueryArgument<IntGraphType> { Name = "skip", Description = "How many games to skip." },
                   new QueryArgument<IntGraphType> { Name = "take", Description = "How many games to return." }
                ),
               resolve: async context =>
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

                   return await query.ToListAsync();
               }
           );

            FieldAsync<UserType>(
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
