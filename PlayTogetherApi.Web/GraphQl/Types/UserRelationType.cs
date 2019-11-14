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
    public class UserRelationType : ObjectGraphType<UserRelationExtModel>
    {
        public UserRelationType(PlayTogetherDbContext db)
        {
            Name = "Relation";

            Field("date", model => model.Relation.CreatedDate, type: typeof(DateTimeGraphType)).Description("Invitation date.");
            Field("status", model => model.Relation.Status, type: typeof(UserRelationStatusType)).Description("Status of the invitation.");

            // todo: this will be the graphtype for all relations; not the specialized one for those with the friend-status

            /*
            FieldAsync<UserType>("user", resolve: async context => {
                if (context.Source.User != null)
                    return context.Source.User;
                return await db.Users.FirstOrDefaultAsync(u => u.UserId == context.Source.UserId);
            });
            */
        }
    }
}
