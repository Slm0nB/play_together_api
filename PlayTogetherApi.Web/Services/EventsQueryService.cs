using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Web.Services
{
    public class EventsQueryService
    {
        public Guid? UserId;
        public List<Guid> FriendIds;
        public Guid[] CreatedByUserIds;
        public Guid[] GameIds;

        public bool OnlyPrivateFilter;
        public bool OnlyByFriendsFilter;
        public bool OnlyByUsersFilter;
        public bool OnlyGamesFilter;
        public bool OnlyJoinedFilter;

        public bool IncludePrivateFilter;
        public bool IncludeByFriendsFilter;
        public bool IncludeByUsersFilter;
        public bool IncludeGamesFilter;
        public bool IncludeJoinedFilter;

        IQueryable<Event> ProcessExclusiveFilter(IQueryable<Event> query)
        {
            if (OnlyPrivateFilter)
            {
                query = query.Where(n => n.FriendsOnly == true);
            }

            if (OnlyByFriendsFilter)
            {
                query = query.Where(n => FriendIds.Contains(n.CreatedByUserId));
            }

            if (OnlyByUsersFilter && CreatedByUserIds != null && CreatedByUserIds.Any())
            {
                var first = CreatedByUserIds.First();
                query = CreatedByUserIds.Count() == 1
                         ? query.Where(n => n.CreatedByUserId == first)
                         : query.Where(n => CreatedByUserIds.Contains(n.CreatedByUserId));
            }

            if (OnlyGamesFilter && GameIds != null && GameIds.Any())
            {
                var first = GameIds.First();
                query = GameIds.Count() == 1
                         ? query.Where(n => n.GameId == first)
                         : query.Where(n => n.GameId.HasValue && GameIds.Contains(n.GameId.Value));
            }

            if (OnlyJoinedFilter)
            {
                query = query.Where(n => n.Signups.Any(nn => nn.UserId == UserId));
            }

            return query;
        }

        IQueryable<Event> ProcessInclusiveFilter(IQueryable<Event> query)
        {
            FriendIds = FriendIds ?? new List<Guid>();
            CreatedByUserIds = CreatedByUserIds ?? Array.Empty<Guid>();
            GameIds = GameIds ?? Array.Empty<Guid>();

            bool actualJoinedFilter = IncludeJoinedFilter && UserId.HasValue;
            bool actualByUsersFilter = IncludeByUsersFilter && CreatedByUserIds.Any();
            bool actualGamesFilter = IncludeGamesFilter && GameIds.Any();

            bool anyFilter = IncludePrivateFilter || IncludeByFriendsFilter || actualByUsersFilter || actualByUsersFilter || actualGamesFilter;
            if (!anyFilter)
                return query;

            query = query.Where(n =>
                (OnlyPrivateFilter && n.FriendsOnly)||
                (IncludeByFriendsFilter && FriendIds.Contains(n.CreatedByUserId))  ||
                (actualJoinedFilter && n.Signups.Any(nn => nn.UserId == UserId)) ||
                (actualByUsersFilter && CreatedByUserIds.Contains(n.CreatedByUserId)) ||
                (actualGamesFilter && n.GameId.HasValue && GameIds.Contains(n.GameId.Value))
            );

            return query;
        }

        IQueryable<Event> ProcessDates(IQueryable<Event> query, DateTime? startsBeforeDate, DateTime? startsAfterDate, DateTime? endsBeforeDate, DateTime? endsAfterDate)
        {
            bool dateWasGiven = false;

            if (startsBeforeDate.HasValue && startsBeforeDate != default(DateTime))
            {
                dateWasGiven = true;
                query = query.Where(n => n.EventDate <= startsBeforeDate);
            }

            if (startsAfterDate.HasValue && startsAfterDate != default(DateTime))
            {
                dateWasGiven = true;
                query = query.Where(n => n.EventDate >= startsAfterDate);
            }

            if (endsBeforeDate.HasValue && endsBeforeDate != default(DateTime))
            {
                dateWasGiven = true;
                query = query.Where(n => n.EventEndDate <= endsBeforeDate);
            }

            if ((!endsAfterDate.HasValue || endsAfterDate == default(DateTime)) && !dateWasGiven)
            {
                endsAfterDate = DateTime.UtcNow;
            }

            if (endsAfterDate != default(DateTime))
            {
                query = query.Where(n => n.EventEndDate >= endsAfterDate);
            }

            return query;
        }

        public IQueryable<Event> Process(IQueryable<Event> query)
        {
            if (UserId.HasValue)
            {
                // Authenticated users get an additional criteria to include friendsonly-events
                query = query.Where(n => !n.FriendsOnly || n.CreatedByUserId == UserId.Value || FriendIds.Contains(n.CreatedByUserId));
            }
            else
            {
                // Unauthenticated users never see friendsonly-events
                query = query.Where(n => !n.FriendsOnly);
            }

            query = ProcessExclusiveFilter(query);
            query = ProcessInclusiveFilter(query);

            return query;
        }
    }
}
