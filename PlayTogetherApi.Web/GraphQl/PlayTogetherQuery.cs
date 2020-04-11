using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using GraphQL.Types;
using PlayTogetherApi.Data;
using PlayTogetherApi.Web.GraphQl.Types;
using PlayTogetherApi.Web.Models;

namespace PlayTogetherApi.Web.GraphQl
{
    public class PlayTogetherQuery : ObjectGraphType
    {
        public PlayTogetherQuery(PlayTogetherDbContext db)
        {
            Name = "Query";

            FieldAsync<EventCollectionGraphType>(
               "events",
                arguments: new QueryArguments(
                   new QueryArgument<StringGraphType> { Name = "id", Description = "Id of the event." },
                   new QueryArgument<StringGraphType> { Name = "search", Description = "Search term applied to the title or description." },
                   new QueryArgument<DateTimeGraphType> { Name = "startsBeforeDate", Description = "Event starts before or on this datetime." },
                   new QueryArgument<DateTimeGraphType> { Name = "startsAfterDate", Description = "Event starts on or after this datetime." },
                   new QueryArgument<DateTimeGraphType> { Name = "endsBeforeDate", Description = "Event ends before or on this datetime." },
                   new QueryArgument<DateTimeGraphType> { Name = "endsAfterDate", Description = "Event ends on or after this datetime. If no start/end arguments are given, this default to 'now'." },
                   new QueryArgument<BooleanGraphType> { Name = "onlyPrivate", Description = "Only show events that are friends-only." },
                   new QueryArgument<BooleanGraphType> { Name = "onlyByFriends", Description = "Only show events that are created by friends. This requires the caller to be authorized." },
                   new QueryArgument<ListGraphType<NonNullGraphType<IdGraphType>>> { Name = "onlyByUsers", Description = "Only show events created by these users." },
                   new QueryArgument<ListGraphType<NonNullGraphType<IdGraphType>>> { Name = "onlyGames", Description = "Only show events for these games." },
                   new QueryArgument<IntGraphType> { Name = "skip", Description = "How many events to skip." },
                   new QueryArgument<IntGraphType> { Name = "take", Description = "How many events to return. Maximum 100.", DefaultValue = 100 }
                ),
               resolve: async context =>
               {
                   IQueryable<Event> query = db.Events;

                   List<UserRelation> friends = null;
                   List<Guid> friendIds = null;
                   var principal = context.UserContext as ClaimsPrincipal;
                   var userIdClaim = principal.Claims.FirstOrDefault(n => n.Type == "userid")?.Value;
                   if (Guid.TryParse(userIdClaim, out var userId))
                   {
                       // Authenticated users get an additional criteria to include friendsonly-events
                       friends = await db.UserRelations.Where(
                           rel => rel.Status == (UserRelationInternalStatus.A_Befriended | UserRelationInternalStatus.B_Befriended) &&
                            (rel.UserAId == userId || rel.UserBId == userId))
                           .ToListAsync();
                       friendIds = friends.Select(rel => rel.UserAId == userId ? rel.UserBId : rel.UserAId).ToList();

                       query = query.Where(n => !n.FriendsOnly || n.CreatedByUserId == userId || friendIds.Contains(n.CreatedByUserId));
                   }
                   else
                   {
                       // Unauthenticated users never see friendsonly-events
                       query = query.Where(n => !n.FriendsOnly);
                   }

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

                   bool dateWasGiven = false;
                   var startsBeforeDate = context.GetArgument<DateTime>("startsBeforeDate");
                   if (startsBeforeDate != default(DateTime))
                   {
                       dateWasGiven = true;
                       query = query.Where(n => n.EventDate <= startsBeforeDate);
                   }
                   var startsAfterDate = context.GetArgument<DateTime>("startsAfterDate");
                   if (startsAfterDate != default(DateTime))
                   {
                       dateWasGiven = true;
                       query = query.Where(n => n.EventDate >= startsAfterDate);
                   }
                   var endsBeforeDate = context.GetArgument<DateTime>("endsBeforeDate");
                   if (endsBeforeDate != default(DateTime))
                   {
                       dateWasGiven = true;
                       query = query.Where(n => n.EventEndDate <= endsBeforeDate);
                   }
                   var endsAfterDate = context.GetArgument<DateTime>("endsAfterDate");
                   if (endsAfterDate == default(DateTime) && !dateWasGiven)
                   {
                       endsAfterDate = DateTime.UtcNow;
                   }
                   if (endsAfterDate != default(DateTime))
                   {
                       query = query.Where(n => n.EventEndDate >= endsAfterDate);
                   }



                   if(context.HasArgument("onlyPrivate"))
                   {
                       var onlyPrivate = context.GetArgument<bool>("onlyPrivate");
                       if (onlyPrivate)
                       {
                           query = query.Where(n => n.FriendsOnly == true);
                       }
                   }


                   if (context.HasArgument("onlyByFriends"))
                   {
                       var onlyByFriends = context.GetArgument<bool>("onlyByFriends");
                       if (onlyByFriends && friendIds != null)
                       {
                           query = query.Where(n => friendIds.Contains(n.CreatedByUserId));
                       }
                   }


                   if (context.HasArgument("onlyByUsers"))
                   {
                       var onlyByUsers = context.GetArgument<Guid[]>("onlyByUsers");
                       if (onlyByUsers != null && onlyByUsers.Any())
                       {
                           var first = onlyByUsers.First();
                           query = onlyByUsers.Count() == 1
                                    ? query.Where(n => n.CreatedByUserId == first)
                                    : query.Where(n => onlyByUsers.Contains(n.CreatedByUserId));
                       }
                   }


                   if (context.HasArgument("onlyGames"))
                   {
                       var onlyGames = context.GetArgument<Guid[]>("onlyGames");
                       if (onlyGames != null && onlyGames.Any())
                       {
                           var first = onlyGames.First();
                           query = onlyGames.Count() == 1
                                    ? query.Where(n => n.GameId == first)
                                    : query.Where(n => n.GameId.HasValue && onlyGames.Contains(n.GameId.Value));
                       }
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

            Field<UserCollectionGraphType>(
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

            Field<GameCollectionGraphType>(
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

            Field<BuiltinAvatarCollectionGraphType>(
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

            FieldAsync<SelfUserGraphType>(
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
