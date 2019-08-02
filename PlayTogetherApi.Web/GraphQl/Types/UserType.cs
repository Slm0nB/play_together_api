using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;
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
        }
    }
}
