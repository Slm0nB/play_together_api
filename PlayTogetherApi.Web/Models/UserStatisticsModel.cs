using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlayTogetherApi.Web.Models
{
    public class UserStatisticsModel
    {
        public Guid UserId;

        public DateTime ExpiresOn;

        public int FriendsCurrentCount;
        public int EventsCreatedTotalCount;
        public int EventsCompletedTodayCount;
        public int EventsCompletedTotalCount;

    }
}
