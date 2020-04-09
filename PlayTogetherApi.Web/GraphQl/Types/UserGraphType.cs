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
    public class UserGraphType : UserPreviewGraphType
    {
        public UserGraphType(PlayTogetherDbContext db, IConfiguration config) : base(db, config)
        {
            Name = "User";

            Field<EventCollectionGraphType>("events",
                arguments: new QueryArguments(
                   new QueryArgument<DateTimeGraphType> { Name = "startsBeforeDate", Description = "Event starts before or on this datetime." },
                   new QueryArgument<DateTimeGraphType> { Name = "startsAfterDate", Description = "Event starts on or after this datetime." },
                   new QueryArgument<DateTimeGraphType> { Name = "endsBeforeDate", Description = "Event ends before or on this datetime." },
                   new QueryArgument<DateTimeGraphType> { Name = "endsAfterDate", Description = "Event ends on or after this datetime. If no start/end arguments are given, this default to 'now'." },
                   new QueryArgument<IntGraphType> { Name = "skip", Description = "How many items to skip." },
                   new QueryArgument<IntGraphType> { Name = "take", Description = "How many items to return." }
                ),
                resolve: context => {
                    var query = db.Events.Where(n => n.CreatedByUserId == context.Source.UserId);

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

            Field<UserEventSignupCollectionGraphType>("signups",
                arguments: new QueryArguments(
                   new QueryArgument<DateTimeGraphType> { Name = "startsBeforeDate", Description = "Event starts before or on this datetime." },
                   new QueryArgument<DateTimeGraphType> { Name = "startsAfterDate", Description = "Event starts on or after this datetime." },
                   new QueryArgument<DateTimeGraphType> { Name = "endsBeforeDate", Description = "Event ends before or on this datetime." },
                   new QueryArgument<DateTimeGraphType> { Name = "endsAfterDate", Description = "Event ends on or after this datetime. If no start/end arguments are given, this default to 'now'." },
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

                    bool dateWasGiven = false;
                    var startsBeforeDate = context.GetArgument<DateTime>("startsBeforeDate");
                    if (startsBeforeDate != default(DateTime))
                    {
                        dateWasGiven = true;
                        signups = signups.Where(n => n.Event.EventDate <= startsBeforeDate);
                    }
                    var startsAfterDate = context.GetArgument<DateTime>("startsAfterDate");
                    if (startsAfterDate != default(DateTime))
                    {
                        dateWasGiven = true;
                        signups = signups.Where(n => n.Event.EventDate >= startsAfterDate);
                    }
                    var endsBeforeDate = context.GetArgument<DateTime>("endsBeforeDate");
                    if (endsBeforeDate != default(DateTime))
                    {
                        dateWasGiven = true;
                        signups = signups.Where(n => n.Event.EventEndDate <= endsBeforeDate);
                    }
                    var endsAfterDate = context.GetArgument<DateTime>("endsAfterDate");
                    if (endsAfterDate == default(DateTime) && !dateWasGiven)
                    {
                        endsAfterDate = DateTime.UtcNow;
                    }
                    if (endsAfterDate != default(DateTime))
                    {
                        signups = signups.Where(n => n.Event.EventEndDate >= endsAfterDate);
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

            Field<UserRelationCollectionGraphType>("friends",
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
    }
}
