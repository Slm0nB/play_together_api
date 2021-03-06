﻿using System;
using GraphQL.Types;
using PlayTogetherApi.Models;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class UserStatisticsGraphType : ObjectGraphType<UserStatisticsModel>
    {
        public UserStatisticsGraphType()
        {
            Name = "UserStatistics";

            Field("id", user => user.UserId, type: typeof(IdGraphType)).Description("Id property from the user object.");
            Field("expiresOn", user => user.ExpiresOn, type: typeof(NonNullGraphType<DateTimeGraphType>));
            Field("friendsCurrentCount", user => user.FriendsCurrentCount, type: typeof(NonNullGraphType<IntGraphType>));
            Field("eventsCreatedTotalCount", user => user.EventsCreatedTotalCount, type: typeof(NonNullGraphType<IntGraphType>));
            Field("eventsCompletedTodayCount", user => user.EventsCompletedTodayCount, type: typeof(NonNullGraphType<IntGraphType>));
            Field("eventsCompletedTotalCount", user => user.EventsCompletedTotalCount, type: typeof(NonNullGraphType<IntGraphType>));
            Field("eventsPendingTodayCount", user => user.EventsPendingTodayCount, type: typeof(NonNullGraphType<IntGraphType>));
            Field("eventsPendingTotalCount", user => user.EventsPendingTotalCount, type: typeof(NonNullGraphType<IntGraphType>));
        }
    }
}
