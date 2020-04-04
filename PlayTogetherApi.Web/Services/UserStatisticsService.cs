using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PlayTogetherApi.Data;
using PlayTogetherApi.Web.Models;

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

        public async Task UpdateStatisticsAsync(PlayTogetherDbContext db, Guid userId)
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

            var model = await BuildStatisticsForUserAsync(db, userId);
            UserStatisticsCache[userId] = new AsyncLazy<UserStatisticsModel>(() => model);

            if(oldModel != null)
            {
                var modelsAreIdentical = oldModel.ExpiresOn == model.ExpiresOn
                    && oldModel.EventsCompletedTodayCount == model.EventsCompletedTodayCount
                    && oldModel.EventsCompletedTotalCount == model.EventsCompletedTotalCount
                    && oldModel.EventsCreatedTotalCount == model.EventsCreatedTotalCount
                    && oldModel.FriendsCurrentCount == model.FriendsCurrentCount;

                if (modelsAreIdentical)
                    return;
            }

            stream.OnNext(model);
        }

        public async Task<UserStatisticsModel> GetOrBuildStatisticsForUser(PlayTogetherDbContext db, Guid userId)
        {
            var model = await UserStatisticsCache.GetOrAdd(userId, _ => new AsyncLazy<UserStatisticsModel>(() => BuildStatisticsForUserAsync(db, userId)));
            if(model.ExpiresOn < DateTime.Now)
            {
                UserStatisticsCache.TryRemove(userId, out var _);
                model = await UserStatisticsCache.GetOrAdd(userId, _ => new AsyncLazy<UserStatisticsModel>(() => BuildStatisticsForUserAsync(db, userId)));
            }
            return model;
        }

        public async Task<UserStatisticsModel> BuildStatisticsForUserAsync(PlayTogetherDbContext db, Guid userId)
        {
            var today = DateTime.Today; // todo: these should be for the users timezone
            var tomorrow = today.AddDays(1);
            var now = DateTime.Now;

            DateTime? expiration = null;
            var nextCreatedEvent = await db.Events.Where(n => n.CreatedByUserId == userId && n.EventEndDate > now).OrderBy(n => n.EventEndDate).FirstOrDefaultAsync();
            expiration = nextCreatedEvent?.EventEndDate;
            var nextSignupEvent = await db.UserEventSignups.Where(n => n.UserId == userId && n.Event.EventEndDate > now).OrderBy(n => n.Event.EventEndDate).Select(n => n.Event).FirstOrDefaultAsync();
            if(nextSignupEvent != null && (!expiration.HasValue || expiration > nextSignupEvent.EventEndDate))
            {
                expiration = nextSignupEvent.EventEndDate;
            }
            if(!expiration.HasValue || expiration > tomorrow)
            {
                expiration = tomorrow;
            }

            var model = new UserStatisticsModel
            {
                UserId = userId,
                ExpiresOn = expiration.Value,
                FriendsCurrentCount = await db.UserRelations.Where(n => n.Status == FriendLogicService.Relation_MutualFriends && (n.UserAId == userId || n.UserBId == userId)).CountAsync(),
                EventsCreatedTotalCount = await db.Events.Where(n => n.CreatedByUserId == userId).CountAsync(),
                EventsCompletedTotalCount = await db.UserEventSignups.Where(n => n.UserId == userId && n.Event.EventEndDate < now).CountAsync()
                                          + await db.Events.Where(n => n.CreatedByUserId == userId && n.EventEndDate < now).CountAsync(),
                EventsCompletedTodayCount = await db.UserEventSignups.Where(n => n.UserId == userId && n.Event.EventEndDate > today && n.Event.EventEndDate < now).CountAsync()
                                          + await db.Events.Where(n => n.CreatedByUserId == userId && n.EventEndDate > today && n.EventEndDate < now).CountAsync(),
            };

            return model;
        }
    }
}
