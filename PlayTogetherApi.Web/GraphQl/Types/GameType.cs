using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using GraphQL.Types;
using PlayTogetherApi.Domain;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class GameType : ObjectGraphType<Game>
    {
        public GameType(PlayTogetherDbContext db, IConfiguration config)
        {
            Field("id", game => game.GameId, type: typeof(IdGraphType));
            Field(game => game.Title);
            Field("image", game => config.GetValue<string>("AssetPath") + game.ImagePath, type: typeof(StringGraphType));

            Field<ListGraphType<EventType>>("events",
                // todo: filter options

                resolve: x => db.Events.Where(n => n.GameId == x.Source.GameId)
            );
        }
    }
}
