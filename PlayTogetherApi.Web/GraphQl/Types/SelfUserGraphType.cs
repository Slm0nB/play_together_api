﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PlayTogetherApi.Data;
using PlayTogetherApi.Services;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class SelfUserGraphType : UserGraphType
    {
        public SelfUserGraphType(PlayTogetherDbContext db, IConfiguration config, UserStatisticsService statisticsService) : base(db, config)
        {
            Name = "SelfUser";

            Field(user => user.Email).Description("Email property from the user object.");

            FieldAsync<UserStatisticsGraphType>("statistics", resolve: async context => {
                var statistics = await statisticsService.GetOrBuildStatisticsForUser(db, context.Source.UserId);
                return statistics;
            });
        }
    }
}