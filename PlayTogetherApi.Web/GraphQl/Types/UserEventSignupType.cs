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
    public class UserEventSignupType : ObjectGraphType<UserEventSignup>
    {
        public UserEventSignupType(PlayTogetherDbContext db)
        {
            Field("date", signup => signup.SignupDate, type: typeof(DateGraphType)).Description("Signup/invitation date.");
            Field("status", signup => signup.Status, type: typeof(UserEventStatusType)).Description("Status of the signup/invitation.");

            FieldAsync<EventType>("event", resolve: async context => {
                if (context.Source.Event != null)
                    return context.Source.Event;
                return await db.Events.FirstOrDefaultAsync(ev => ev.EventId == context.Source.EventId);
            });

            FieldAsync<UserType>("user", resolve: async context => {
                if (context.Source.User != null)
                    return context.Source.User;
                return await db.Users.FirstOrDefaultAsync(u => u.UserId == context.Source.UserId);
            });

        }
    }
}
