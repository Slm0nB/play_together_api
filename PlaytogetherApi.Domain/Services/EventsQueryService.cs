using System;
using System.Collections.Generic;
using System.Linq;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Web.Services
{
    /// <summary>
    /// This service is used both to modify queries against the database, as well as for filtering events in memory.
    /// </summary>
    public class EventsQueryService
    {
        public Guid? UserId;
        public List<Guid> FriendIds;

        public string SearchTerm;

        public DateTime? StartsBeforeDate;
        public DateTime? StartsAfterDate;
        public DateTime? EndsBeforeDate;
        public DateTime? EndsAfterDate;

        public bool OnlyPrivateFilter;
        public bool OnlyByFriendsFilter;
        public Guid[] OnlyByUsersFilter;
        public Guid[] OnlyGamesFilter;
        public bool OnlyJoinedFilter;
        public bool OnlyJoinedByFriendsFilter;

        public bool IncludePrivateFilter;
        public bool IncludeByFriendsFilter;
        public Guid[] IncludeByUsersFilter;
        public Guid[] IncludeGamesFilter;
        public bool IncludeJoinedFilter;
        public bool IncludeJoinedByFriendsFilter;

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

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var search = SearchTerm.ToLowerInvariant();
                query = query.Where(n => n.Title.ToLower().Contains(search) || n.Description.ToLower().Contains(search)); // todo: verify sql isnt retarded
            }

            query = ProcessDates(query);
            query = ProcessExclusiveFilter(query);
            query = ProcessInclusiveFilter(query);

            return query;
        }

        public IQueryable<Event> ProcessDates(IQueryable<Event> query)
        {
            bool dateWasGiven = false;

            if (StartsBeforeDate.HasValue && StartsBeforeDate != default(DateTime))
            {
                dateWasGiven = true;
                query = query.Where(n => n.EventDate <= StartsBeforeDate);
            }

            if (StartsAfterDate.HasValue && StartsAfterDate != default(DateTime))
            {
                dateWasGiven = true;
                query = query.Where(n => n.EventDate >= StartsAfterDate);
            }

            if (EndsBeforeDate.HasValue && EndsBeforeDate != default(DateTime))
            {
                dateWasGiven = true;
                query = query.Where(n => n.EventEndDate <= EndsBeforeDate);
            }

            var actualEndsAfterDate = EndsAfterDate;
            if ((!actualEndsAfterDate.HasValue || actualEndsAfterDate == default(DateTime)) && !dateWasGiven)
            {
                actualEndsAfterDate = DateTime.UtcNow;
            }
            if (actualEndsAfterDate.HasValue && actualEndsAfterDate != default(DateTime))
            {
                query = query.Where(n => n.EventEndDate >= actualEndsAfterDate);
            }

            return query;
        }

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

            if (OnlyJoinedByFriendsFilter)
            {
                query = query.Where(n => n.Signups.Any(nn => FriendIds.Contains(nn.UserId))); // todo: might need to read the status in the future
            }

            if (OnlyByUsersFilter != null && OnlyByUsersFilter.Any())
            {
                var first = OnlyByUsersFilter.First();
                query = OnlyByUsersFilter.Count() == 1
                         ? query.Where(n => n.CreatedByUserId == first)
                         : query.Where(n => OnlyByUsersFilter.Contains(n.CreatedByUserId));
            }

            if (OnlyGamesFilter != null && OnlyGamesFilter.Any())
            {
                var first = OnlyGamesFilter.First();
                query = OnlyGamesFilter.Count() == 1
                         ? query.Where(n => n.GameId == first)
                         : query.Where(n => n.GameId.HasValue && OnlyGamesFilter.Contains(n.GameId.Value));
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
            IncludeByUsersFilter = IncludeByUsersFilter ?? Array.Empty<Guid>();
            IncludeGamesFilter = IncludeGamesFilter ?? Array.Empty<Guid>();

            bool actualJoinedFilter = IncludeJoinedFilter && UserId.HasValue;
            bool actualByUsersFilter = IncludeByUsersFilter.Any();
            bool actualGamesFilter = IncludeGamesFilter.Any();

            bool anyFilter = IncludePrivateFilter || IncludeByFriendsFilter || IncludeJoinedByFriendsFilter || actualByUsersFilter || actualJoinedFilter || actualGamesFilter;
            if (!anyFilter)
                return query;

            query = query.Where(n =>
                (OnlyPrivateFilter && n.FriendsOnly)||
                (IncludeByFriendsFilter && FriendIds.Contains(n.CreatedByUserId))  ||
                (IncludeJoinedByFriendsFilter && n.Signups.Any(nn => FriendIds.Contains(nn.UserId))) ||
                (actualJoinedFilter && n.Signups.Any(nn => nn.UserId == UserId)) ||
                (actualByUsersFilter && IncludeByUsersFilter.Contains(n.CreatedByUserId)) ||
                (actualGamesFilter && n.GameId.HasValue && IncludeGamesFilter.Contains(n.GameId.Value))
            );

            return query;
        }
    }
}
