using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PlayTogetherApi.Data;
using PlayTogetherApi.Services;
using PlayTogetherApi.Web.Models;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class SystemStatisticsGraphType : ObjectGraphType
    {
        public SystemStatisticsGraphType(ObservablesService observablesService)
        {
            Name = "SystemStatistics";

            Field<IntGraphType>("userStatisticsSubscriptionCount", resolve: context => observablesService.UserStatisticsStreams.Count() );

        }
    }
}
