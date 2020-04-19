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

        public IQueryable<Event> ProcessFilter(IQueryable<Event> query, Guid userId, List<Guid> friendIds, bool onlyPrivateFilter, bool onlyByFriendsFilter, bool onlyJoinedFilter, Guid[] onlyByUserIds, Guid[] onlyGameIds)
        {
            friendIds = friendIds ?? new List<Guid>();
            onlyByFriendsFilter = onlyByFriendsFilter && friendIds.Any();

            onlyByUserIds = onlyByUserIds ?? new Guid[0];
            var onlyByUsersFilter = onlyByUserIds.Any();

            onlyGameIds = onlyGameIds ?? new Guid[0];
            var onlyGamesFilter = onlyGameIds.Any();

            bool anyFilter = onlyPrivateFilter || onlyByFriendsFilter || onlyJoinedFilter || onlyByUsersFilter || onlyGamesFilter;
            if (!anyFilter)
                return query;

            /*
            if (onlyPrivateFilter)
            {
                query = query.Where(n => n.FriendsOnly == true);
            }

            if (onlyByFriendsFilter)
            {
                query = query.Where(n => friendIds.Contains(n.CreatedByUserId));
            }

            if (onlyByUsersFilter)
            {
                var first = onlyByUserIds.First();
                query = onlyByUserIds.Count() == 1
                         ? query.Where(n => n.CreatedByUserId == first)
                         : query.Where(n => onlyByUserIds.Contains(n.CreatedByUserId));
            }

            if (onlyGamesFilter)
            {
                var first = onlyGameIds.First();
                query = onlyGameIds.Count() == 1
                         ? query.Where(n => n.GameId == first)
                         : query.Where(n => n.GameId.HasValue && onlyGameIds.Contains(n.GameId.Value));
            }

            if (onlyJoined)
            {
                query = query.Where(n => n.Signups.Any(nn => nn.UserId == userId));
            }
            */

            query = query.Where(n =>
                (onlyPrivateFilter && n.FriendsOnly)||
                (onlyByFriendsFilter && friendIds.Contains(n.CreatedByUserId))  ||
                (onlyJoinedFilter && n.Signups.Any(nn => nn.UserId == userId)) ||
                (onlyByUsersFilter && onlyByUserIds.Contains(n.CreatedByUserId)) ||
                (onlyGamesFilter && n.GameId.HasValue && onlyGameIds.Contains(n.GameId.Value))
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

        IQueryable<Event> Process(IQueryable<Event> query)
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


            return query;
        }
    }
}
