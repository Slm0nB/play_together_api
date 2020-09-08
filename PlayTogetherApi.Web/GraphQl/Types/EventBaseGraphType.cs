using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GraphQL.Types;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    /// <summary>
    /// This was originally made for the Event subscription, to be able to exclude "signups".  But we dropped that idea.
    /// </summary>
    public class EventBaseGraphType : ObjectGraphType<Event>
    {
        public EventBaseGraphType()
        {
            Name = "EventBase";

            Field("id", x => x.EventId, type: typeof(IdGraphType)).Description("Id property from the event object.");
            Field(x => x.CreatedDate, type: typeof(NonNullGraphType<DateTimeGraphType>)).Description("When the event was created.");
            Field<DateTimeGraphType>("startDate", resolve: context => context.Source.EventDate, description: "When the event starts.");
            Field<DateTimeGraphType>("endDate", resolve: context => context.Source.EventEndDate, description: "When the event ends.");
            Field(x => x.EventEndDate, type: typeof(DateTimeGraphType)).Description("When the event ends.").DeprecationReason("WTF, this is a duplicate of 'endDate'.  How did that happen.");
            Field(x => x.Title).Description("Title of the event.");
            Field(x => x.Description, type: typeof(StringGraphType)).Description("Description of the event.");
            Field(x => x.FriendsOnly).Description("If the event is only visible to friends of the creator.");
            Field(x => x.CallToArms).Description("If the event is a call to arms.");

            FieldAsync<UserGraphType>("author",
                resolve: async context =>
                {
                    if (context.Source.CreatedByUser != null)
                        return context.Source.CreatedByUser;

                    var db = context.RequestServices.GetService<PlayTogetherDbContext>();
                    return await db.Users.FirstOrDefaultAsync(n => n.UserId == context.Source.CreatedByUserId);
                }
            );

            FieldAsync<GameGraphType>("game",
                resolve: async context =>
                {
                    if (context.Source.Game != null)
                        return context.Source.Game;

                    var db = context.RequestServices.GetService<PlayTogetherDbContext>();
                    return await db.Games.FirstOrDefaultAsync(n => n.GameId == context.Source.GameId);
                }
            );
        }
    }
}
