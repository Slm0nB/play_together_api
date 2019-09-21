using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using PlayTogetherApi.Domain;
using PlayTogetherApi.Web.Models;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class UserType : ObjectGraphType<User>
    {
        public UserType(PlayTogetherDbContext db)
        {
            Field("id", user => user.UserId, type: typeof(IdGraphType)).Description("Id property from the user object.");
            Field(user => user.DisplayName).Description("DisplayName property from the user object.");

            Field<StringGraphType>("avatar",
                arguments: new QueryArguments(
                    new QueryArgument<IntGraphType> { Name = "width", DefaultValue = 128 }
                ),
                resolve: context =>
                {
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
                   new QueryArgument<IntGraphType> { Name = "skip", Description = "How many events to skip." },
                   new QueryArgument<IntGraphType> { Name = "take", Description = "How many events to return." }
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

            FieldAsync<ListGraphType<UserEventSignupType>>("signups",
                arguments: new QueryArguments(
                   new QueryArgument<DateTimeGraphType> { Name = "beforeDate", Description = "Event occurs before or on this datetime." },
                   new QueryArgument<DateTimeGraphType> { Name = "afterDate", Description = "Event occurs on or after this datetime." },
                   new QueryArgument<IntGraphType> { Name = "skip", Description = "How many events to skip." },
                   new QueryArgument<IntGraphType> { Name = "take", Description = "How many events to return." }
                ),
                resolve: async context =>
                {
                    var userId = context.Source.UserId;
                    var signups = await db.UserEventSignups
                        .Where(n => n.UserId == userId)
                        .Include(n => n.Event)
                        .OrderBy(n => n.SignupDate)
                        .ToListAsync();

                    var afterDate = context.GetArgument<DateTime>("afterDate");
                    if (afterDate != default(DateTime))
                    {
                        signups = signups.Where(n => n.Event.EventEndDate >= afterDate).ToList();
                    }

                    var beforeDate = context.GetArgument<DateTime>("beforeDate");
                    if (beforeDate != default(DateTime))
                    {
                        signups = signups.Where(n => n.Event.EventDate <= beforeDate).ToList();
                    }

                    var skip = context.GetArgument<int>("skip");
                    if (skip > 0)
                    {
                        signups = signups.Skip(skip).ToList();
                    }

                    var take = context.GetArgument<int>("take");
                    if (take > 0)
                    {
                        signups = signups.Take(take).ToList();
                    }

                    return signups;
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
