using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using GraphQL.Types;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class BuiltinAvatarCollectionGraphType : ObjectGraphType<IQueryable<BuiltinAvatar>>
    {
        public BuiltinAvatarCollectionGraphType(PlayTogetherDbContext db, IConfiguration config)
        {
            Name = "AvatarCollection";

            FieldAsync<IntGraphType>("total",
                description: "The total number of avatars available",
                resolve: async context =>
                {
                    var total = await db.Avatars.CountAsync();
                    return total;
                }
            );

            FieldAsync<IntGraphType>("count",
                description: "The number of avatars selected by the query",
                resolve: async context =>
                {
                    var count = await context.Source.CountAsync();
                    return count;
                }
            );

            FieldAsync<ListGraphType<BuiltinAvatarGraphType>>("items",
                resolve: async context => await context.Source.ToListAsync());
        }
    }
}
