using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PlayTogetherApi.Data;
using PlayTogetherApi.Web.Models;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class UserType : ObjectGraphType<User>
    {
        public UserType(PlayTogetherDbContext db, IConfiguration config)
        {
            Field("id", user => user.UserId, type: typeof(IdGraphType)).Description("Id property from the user object.");
            Field(user => user.DisplayName).Description("DisplayName property from the user object.");

            Field<StringGraphType>("avatar",
                arguments: new QueryArguments(
                    new QueryArgument<IntGraphType> { Name = "width", DefaultValue = 128 }
                ),
                resolve: context =>
                {
                    if(!string.IsNullOrWhiteSpace(context.Source.AvatarFilename))
                    {
                        // todo: handle width, or deliberately ignore?

                        var url = config.GetValue<string>("AssetPath") + context.Source.AvatarFilename;
                        return url;
                    }

                    var width = context.GetArgument<int>("width", 128);
                    var hash = md5(context.Source.Email);
                    return $"http://gravatar.com/avatar/{hash}?s={width}&d=mm";
                },
                description: "Url of the avatar image."
            );

            Field<EventCollectionType>("events",
                arguments: new QueryArguments(
                   new QueryArgument<DateTimeGraphType> { Name = "beforeDate", Description = "Event occurs before or on this datetime." },
                   new QueryArgument<DateTimeGraphType> { Name = "afterDate", Description = "Event occurs on or after this datetime." },
                   new QueryArgument<IntGraphType> { Name = "skip", Description = "How many items to skip." },
                   new QueryArgument<IntGraphType> { Name = "take", Description = "How many items to return." }
                ),
                resolve: context => {
                    var query = db.Events.Where(n => n.CreatedByUserId == context.Source.UserId);

                    var afterDate = context.GetArgument<DateTime>("afterDate");
                    if (afterDate != default(DateTime))
                    {
                        query = query.Where(n => n.EventEndDate >= afterDate);
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

                    var take = context.GetArgument<int>("take");
                    if (take > 0)
                    {
                        query = query.Take(take);
                    }

                    return new EventCollectionModel
                    {
                        EventsQuery = query,
                        TotalEventsQuery = db.Events.Where(n => n.CreatedByUserId == context.Source.UserId)
                    };
                }
            );

            Field<UserEventSignupCollectionType>("signups",
                arguments: new QueryArguments(
                   new QueryArgument<DateTimeGraphType> { Name = "beforeDate", Description = "Event occurs before or on this datetime." },
                   new QueryArgument<DateTimeGraphType> { Name = "afterDate", Description = "Event occurs on or after this datetime." },
                   new QueryArgument<IntGraphType> { Name = "skip", Description = "How many items to skip." },
                   new QueryArgument<IntGraphType> { Name = "take", Description = "How many items to return." }
                ),
                resolve: context =>
                {
                    var userId = context.Source.UserId;
                    IQueryable<UserEventSignup> signups = db.UserEventSignups
                        .Where(n => n.UserId == userId)
                        .Include(n => n.Event)
                        .OrderBy(n => n.SignupDate);

                    var afterDate = context.GetArgument<DateTime>("afterDate");
                    if (afterDate != default(DateTime))
                    {
                        signups = signups.Where(n => n.Event.EventEndDate >= afterDate);
                    }

                    var beforeDate = context.GetArgument<DateTime>("beforeDate");
                    if (beforeDate != default(DateTime))
                    {
                        signups = signups.Where(n => n.Event.EventDate <= beforeDate);
                    }

                    var skip = context.GetArgument<int>("skip");
                    if (skip > 0)
                    {
                        signups = signups.Skip(skip);
                    }

                    var take = context.GetArgument<int>("take");
                    if (take > 0)
                    {
                        signups = signups.Take(take);
                    }

                    return new UserEventSignupCollectionModel
                    {
                        ItemsQuery = signups,
                        TotalItemsQuery = db.UserEventSignups.Where(n => n.UserId == context.Source.UserId)
                    };
                }
            );

            Field<UserRelationCollectionType>("friends",
                arguments: new QueryArguments(
                   new QueryArgument<IntGraphType> { Name = "skip", Description = "How many items to skip." },
                   new QueryArgument<IntGraphType> { Name = "take", Description = "How many items to return." }
                ),
                resolve: context =>
                {
                    var userId = context.Source.UserId;
                    IQueryable<UserRelation> relations = db.UserRelations
                        .Where(relation => /*relation.Status == (UserRelationInternalStatus.A_Befriended | UserRelationInternalStatus.B_Befriended) &&*/ (relation.UserBId == context.Source.UserId || relation.UserAId == context.Source.UserId))
                        .OrderBy(relation => relation.CreatedDate);

                    IQueryable<UserRelation> filteredRelations = relations
                        .Include(n => n.UserA)
                        .Include(n => n.UserB);

                    var skip = context.GetArgument<int>("skip");
                    if (skip > 0)
                    {
                        filteredRelations = filteredRelations.Skip(skip);
                    }

                    var take = context.GetArgument<int>("take");
                    if (take > 0)
                    {
                        filteredRelations = filteredRelations.Take(take);
                    }

                    return new UserRelationCollectionModel
                    {
                        UserId = userId,
                        ItemsQuery = filteredRelations,
                        TotalItemsQuery = relations
                    };
                }
            );
        }

        private string md5(string text)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var hashedBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(text));
                var hash = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
                return hash;
            }
        }
    }
}
