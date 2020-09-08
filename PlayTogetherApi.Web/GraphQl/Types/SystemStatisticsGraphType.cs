using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using GraphQL.Types;
using PlayTogetherApi.Services;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class SystemStatisticsGraphType : ObjectGraphType
    {
        public SystemStatisticsGraphType(ObservablesService observablesService)
        {
            Name = "SystemStatistics";

            Field<IntGraphType>("userStatisticsSubscriptionCount",
                resolve: context =>
                {
                    return observablesService.UserStatisticsStreams.Count();
                });

        }
    }
}
