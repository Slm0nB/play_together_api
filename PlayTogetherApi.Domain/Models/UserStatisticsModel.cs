using System;

namespace PlayTogetherApi.Models
{
    public class UserStatisticsModel
    {
        public Guid UserId;

        public DateTime ExpiresOn;

        public int FriendsCurrentCount;
        public int EventsCreatedTotalCount;
        public int EventsCompletedTodayCount;
        public int EventsCompletedTotalCount;
        public int EventsPendingTodayCount;
        public int EventsPendingTotalCount;

    }
}
