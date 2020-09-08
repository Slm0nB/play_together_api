using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GraphQL.Types;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class UserCollectionGraphType : ObjectGraphType<IQueryable<User>>
    {
        public UserCollectionGraphType()
        {
            Name = "UserCollection";

            FieldAsync<IntGraphType>("total",
                description: "The total number of active users",
                resolve: async context =>
                {
                    var db = context.RequestServices.GetService<PlayTogetherDbContext>();

                    var total = await db.Users.Where(n => !n.SoftDelete).CountAsync();
                    return total;
                }
            );

            FieldAsync<IntGraphType>("count",
                description: "The number of users selected by the query",
                resolve: async context =>
                {
                    var db = context.RequestServices.GetService<PlayTogetherDbContext>();

                    var count = await context.Source.CountAsync();
                    return count;
                }
            );

            FieldAsync<ListGraphType<UserGraphType>>("items",
                resolve: async context => await context.Source.ToListAsync());
        }
    }
}
