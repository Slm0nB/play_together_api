using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PlayTogetherApi.Data;
using PlayTogetherApi.Models;

namespace PlayTogetherApi.Services
{
    public class UserStatisticsService
    {
        private readonly ConcurrentDictionary<Guid, AsyncLazy<UserStatisticsModel>> UserStatisticsCache = new ConcurrentDictionary<Guid, AsyncLazy<UserStatisticsModel>>();
        private readonly ObservablesService _observablesService;

        public UserStatisticsService(ObservablesService observablesService)
        {
            _observablesService = observablesService;
        }

        public void InvalidateCache(Guid userId)
        {
            UserStatisticsCache.TryRemove(userId, out var _);
        }

        public async Task UpdateExpiredStatisticsAsync(PlayTogetherDbContext db)
        {
            var userIds = _observablesService.UserStatisticsStreams.Keys.ToArray();

            foreach(var userId in userIds)
            {
                if (UserStatisticsCache.TryGetValue(userId, out var oldModelTask))
                {
                    var oldModel = await oldModelTask;
                    if(oldModel.ExpiresOn > DateTime.UtcNow)
                    {
                        continue; // its not expired
                    }
                }

                var model = await BuildStatisticsForUserAsync(db, userId);
                UserStatisticsCache[userId] = new AsyncLazy<UserStatisticsModel>(() => model);
                var stream = _observablesService.GetUserStatisticsStream(userId, false);
                stream?.OnNext(model);
            }
        }

        public async Task UpdateStatisticsAsync(PlayTogetherDbContext db, Guid userId, User user = null)
        {
            // todo: it's possibly to optimize this slightly, by letting the caller pass in a delegate that recalculates only the fields it knows were changed,
            // but since this only happens when the user performs an action, I'm not concerned now.

            var stream = _observablesService.GetUserStatisticsStream(userId, false);
            if(stream == null)
            {
                InvalidateCache(userId);
                return;
            }

            UserStatisticsModel oldModel = null;
            if(UserStatisticsCache.TryGetValue(userId, out var oldModelTask))
            {
                oldModel = await oldModelTask;
            }

            var model = await BuildStatisticsForUserAsync(db, userId, user);
            UserStatisticsCache[userId] = new AsyncLazy<UserStatisticsModel>(() => model);

            if(oldModel != null)
            {
                var modelsAreIdentical = oldModel.ExpiresOn == model.ExpiresOn
                    && oldModel.EventsCompletedTodayCount == model.EventsCompletedTodayCount
                    && oldModel.EventsCompletedTotalCount == model.EventsCompletedTotalCount
                    && oldModel.EventsPendingTodayCount == model.EventsPendingTodayCount
                    && oldModel.EventsPendingTotalCount == model.EventsPendingTotalCount
                    && oldModel.EventsCreatedTotalCount == model.EventsCreatedTotalCount
                    && oldModel.FriendsCurrentCount == model.FriendsCurrentCount;

                if (modelsAreIdentical)
                    return;
            }

            stream.OnNext(model);
        }

        public async Task<UserStatisticsModel> GetOrBuildStatisticsForUserAsync(PlayTogetherDbContext db, Guid userId, User user = null)
        {
            var model = await UserStatisticsCache.GetOrAdd(userId, _ => new AsyncLazy<UserStatisticsModel>(() => BuildStatisticsForUserAsync(db, userId, user)));
            if(model.ExpiresOn < DateTime.Now)
            {
                UserStatisticsCache.TryRemove(userId, out var _);
                model = await UserStatisticsCache.GetOrAdd(userId, _ => new AsyncLazy<UserStatisticsModel>(() => BuildStatisticsForUserAsync(db, userId, user)));
            }
            return model;
        }

        public async Task<UserStatisticsModel> BuildStatisticsForUserAsync(PlayTogetherDbContext db, Guid userId, User user = null)
        {
            try
            {
                user = user ?? await db.Users.FirstOrDefaultAsync(n => n.UserId == userId);

                var utcOffset = user.UtcOffset ?? TimeSpan.Zero;
                var utcNow = DateTime.UtcNow;
                var userNow = utcNow - utcOffset;
                var userToday = new DateTime(userNow.Year, userNow.Month, userNow.Day, 0, 0, 0);
                var userTomorrow = userToday.AddDays(1);

                var model = new UserStatisticsModel
                {
                    UserId = userId,

                    FriendsCurrentCount = await db.UserRelations.Where(n => n.Status == FriendLogicService.Relation_MutualFriends && (n.UserAId == userId || n.UserBId == userId)).CountAsync(),

                    EventsCreatedTotalCount = await db.Events.Where(n => n.CreatedByUserId == userId).CountAsync(),

                    EventsCompletedTotalCount = await db.UserEventSignups.Where(n => n.UserId == userId && n.Event.EventEndDate < utcNow).CountAsync(),

                    EventsCompletedTodayCount = await db.UserEventSignups.Where(n => n.UserId == userId && n.Event.EventEndDate > userToday && n.Event.EventEndDate < utcNow).CountAsync(),

                    EventsPendingTotalCount = await db.UserEventSignups.Where(n => n.UserId == userId && n.Event.EventEndDate > utcNow).CountAsync(),

                    EventsPendingTodayCount = await db.UserEventSignups.Where(n => n.UserId == userId && n.Event.EventEndDate < userTomorrow && n.Event.EventEndDate > utcNow).CountAsync()
                };

                DateTime? expiration = null;
                var nextCreatedEvent = await db.Events.Where(n => n.CreatedByUserId == userId && n.EventEndDate > utcNow).OrderBy(n => n.EventEndDate).FirstOrDefaultAsync();
                expiration = nextCreatedEvent?.EventEndDate;
                var nextSignupEvent = await db.UserEventSignups.Where(n => n.UserId == userId && n.Event.EventEndDate > utcNow).OrderBy(n => n.Event.EventEndDate).Select(n => n.Event).FirstOrDefaultAsync();
                if (nextSignupEvent != null && (!expiration.HasValue || expiration > nextSignupEvent.EventEndDate))
                {
                    expiration = nextSignupEvent.EventEndDate;
                }
                if (!expiration.HasValue || (expiration > userTomorrow && model.EventsCompletedTodayCount>0))
                {
                    expiration = userTomorrow;
                }
                model.ExpiresOn = expiration.Value;

                return model;
            }
            catch(Exception)
            {
                return null;
            }
        }
    }
}
