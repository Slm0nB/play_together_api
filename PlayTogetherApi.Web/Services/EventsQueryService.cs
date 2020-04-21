using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Web.Services
{
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

        public bool IncludePrivateFilter;
        public bool IncludeByFriendsFilter;
        public Guid[] IncludeByUsersFilter;
        public Guid[] IncludeGamesFilter;
        public bool IncludeJoinedFilter;

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

        public void ReadParametersFromContext(ResolveFieldContext<object> context)
        {
            SearchTerm = context.GetArgument<string>("search");

            StartsBeforeDate = context.HasArgument("startsBeforeDate") ? context.GetArgument<DateTime>("startsBeforeDate") : (DateTime?)null;
            StartsAfterDate = context.HasArgument("startsAfterDate") ? context.GetArgument<DateTime>("startsAfterDate") : (DateTime?)null;
            EndsBeforeDate = context.HasArgument("endsBeforeDate") ? context.GetArgument<DateTime>("endsBeforeDate") : (DateTime?)null;
            EndsAfterDate = context.HasArgument("endsAfterDate") ? context.GetArgument<DateTime>("endsAfterDate") : (DateTime?)null;

            OnlyPrivateFilter = context.HasArgument("onlyPrivate") ? context.GetArgument<bool>("onlyPrivate") : false;
            OnlyByFriendsFilter = context.HasArgument("onlyByFriends") ? context.GetArgument<bool>("onlyByFriends") : false;
            OnlyJoinedFilter = context.HasArgument("onlyJoined") ? context.GetArgument<bool>("onlyJoined") : false;
            OnlyByUsersFilter = context.HasArgument("onlyByUsers") ? context.GetArgument<Guid[]>("onlyByUsers") : null;
            OnlyGamesFilter = context.HasArgument("onlyGames") ? context.GetArgument<Guid[]>("onlyGames") : null;

            IncludePrivateFilter = context.HasArgument("includePrivate") ? context.GetArgument<bool>("includePrivate") : false;
            IncludeByFriendsFilter = context.HasArgument("includeByFriends") ? context.GetArgument<bool>("includeByFriends") : false;
            IncludeJoinedFilter = context.HasArgument("includeJoined") ? context.GetArgument<bool>("includeJoined") : false;
            IncludeByUsersFilter = context.HasArgument("includeByUsers") ? context.GetArgument<Guid[]>("includeByUsers") : null;
            IncludeGamesFilter = context.HasArgument("includeGames") ? context.GetArgument<Guid[]>("includeGames") : null;
        }

        IQueryable<Event> ProcessDates(IQueryable<Event> query)
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

            if (actualEndsAfterDate != default(DateTime))
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
    }
}
