using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    /// <summary>
    /// This was originally made for the Event subscription, to be able to exclude "signups".  But we dropped that idea.
    /// </summary>
    public class EventBaseType : ObjectGraphType<Event>
    {
        public EventBaseType(PlayTogetherDbContext db)
        {
            Field("id", x => x.EventId, type: typeof(IdGraphType)).Description("Id property from the event object.");
            Field(x => x.CreatedDate, type: typeof(NonNullGraphType<DateTimeGraphType>)).Description("When the event was created.");
            Field<DateTimeGraphType>("startDate", resolve: context => context.Source.EventDate, description: "When the event starts.");
            Field<DateTimeGraphType>("endDate", resolve: context => context.Source.EventEndDate, description: "When the event ends.");
            Field(x => x.EventEndDate, type: typeof(DateTimeGraphType)).Description("When the event ends.").DeprecationReason("WTF, this is a duplicate of 'endDate'.  How did that happen.");
            Field(x => x.Title).Description("Title of the event.");
            Field(x => x.Description, type: typeof(StringGraphType)).Description("Description of the event.");
            Field(x => x.FriendsOnly).Description("If the event is only visible to friends of the creator.");
            Field(x => x.CallToArms).Description("If the event is a call to arms.");

            FieldAsync<UserType>("author",
                resolve: async context => context.Source.CreatedByUser ?? await db.Users.FirstOrDefaultAsync(n => n.UserId == context.Source.CreatedByUserId)
            );

            FieldAsync<GameType>("game",
                resolve: async context => context.Source.Game ?? await db.Games.FirstOrDefaultAsync(n => n.GameId == context.Source.GameId)
            );
        }
    }
}
