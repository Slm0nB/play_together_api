using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlayTogetherApi.Data;
using PlayTogetherApi.Services;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class SelfUserGraphType : UserGraphType
    {
        public SelfUserGraphType(IConfiguration config, UserStatisticsService statisticsService) : base(config)
        {
            Name = "SelfUser";

            Field(user => user.Email).Description("Email property from the user object.");

            Field(user => user.DeviceToken).Description("FCM device token.");

            FieldAsync<UserStatisticsGraphType>("statistics", resolve: async context => {
                var db = context.RequestServices.GetService<PlayTogetherDbContext>();

                var statistics = await statisticsService.GetOrBuildStatisticsForUserAsync(db, context.Source.UserId, context.Source);
                return statistics;
            });
        }
    }
}
