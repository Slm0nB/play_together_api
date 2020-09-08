using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GraphQL.Types;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class UserEventSignupGraphType : ObjectGraphType<UserEventSignup>
    {
        public UserEventSignupGraphType()
        {
            Name = "UserEventSignup";

            Field("date", signup => signup.SignupDate, type: typeof(DateTimeGraphType)).Description("Signup/invitation date.");
            Field("status", signup => signup.Status, type: typeof(UserEventStatusGraphType)).Description("Status of the signup/invitation.");

            FieldAsync<EventGraphType>("event", resolve: async context => {
                if (context.Source.Event != null)
                    return context.Source.Event;

                var db = context.RequestServices.GetService<PlayTogetherDbContext>();

                return await db.Events.FirstOrDefaultAsync(ev => ev.EventId == context.Source.EventId);
            });

            FieldAsync<UserGraphType>("user", resolve: async context => {
                if (context.Source.User != null)
                    return context.Source.User;

                var db = context.RequestServices.GetService<PlayTogetherDbContext>();

                return await db.Users.FirstOrDefaultAsync(u => u.UserId == context.Source.UserId);
            });
        }
    }
}
