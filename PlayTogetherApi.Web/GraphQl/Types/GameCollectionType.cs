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
    public class GameCollectionType : ObjectGraphType<IQueryable<Game>>
    {
        public GameCollectionType(PlayTogetherDbContext db, IConfiguration config)
        {
            Name = "GameCollection";

            FieldAsync<IntGraphType>("total",
                description: "The total number of games available",
                resolve: async context =>
                {
                    var total = await db.Games.CountAsync();
                    return total;
                }
            );

            FieldAsync<IntGraphType>("count",
                description: "The number of games selected by the query",
                resolve: async context =>
                {
                    var count = await context.Source.CountAsync();
                    return count;
                }
            );

            FieldAsync<ListGraphType<GameType>>("items",
                resolve: async context => await context.Source.ToListAsync());
        }
    }
}
