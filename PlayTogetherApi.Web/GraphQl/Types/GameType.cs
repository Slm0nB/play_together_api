using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;
using PlayTogetherApi.Domain;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class GameType : ObjectGraphType<Game>
    {
        public GameType(PlayTogetherDbContext db)
        {
            Field("id", x => x.GameId, type: typeof(IdGraphType));
            Field(x => x.Title);
            Field("image", x => x.ImagePath);

            Field<ListGraphType<EventType>>("events",
                // todo: filter options

                resolve: x => db.Events.Where(n => n.GameId == x.Source.GameId)
            );
        }
    }
}
