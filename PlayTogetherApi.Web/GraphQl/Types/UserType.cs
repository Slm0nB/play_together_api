using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using PlayTogetherApi.Domain;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class UserType : ObjectGraphType<User>
    {
        public UserType(PlayTogetherDbContext db)
        {
            Field("id", user => user.UserId, type: typeof(IdGraphType)).Description("Id property from the user object.");
            Field(user => user.DisplayName).Description("DisplayName property from the user object.");
            Field(user => user.Email).Description("Email property from the user object.");

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

            Field<ListGraphType<EventType>>("events",
                // todo: filter options
                resolve: context => db.Events.Where(n => n.CreatedByUserId == context.Source.UserId)
            );

            FieldAsync<ListGraphType<EventType>>("signups",
                resolve: async context =>
                {
                    var userId = context.Source.UserId;
                    var events = await db.UserEventSignups
                        .Where(n => n.UserId == userId)
                        .Include(n => n.Event)
                        .OrderBy(n => n.SignupDate)
                        .Select(n => n.Event)
                        .ToListAsync();
                    return events;
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
