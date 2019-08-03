using System;
using System.Collections.Generic;
using System.Linq;
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
            Field("id", x => x.UserId, type: typeof(IdGraphType)).Description("Id property from the user object.");
            Field(x => x.DisplayName).Description("DisplayName property from the user object.");
            Field(x => x.Email).Description("Email property from the user object.");

            Field<ListGraphType<EventType>>("events",
                // todo: filter options
                resolve: x => db.Events.Where(n => n.CreatedByUserId == x.Source.UserId)
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
    }
}
