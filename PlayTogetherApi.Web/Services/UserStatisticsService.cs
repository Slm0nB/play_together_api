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
        ConcurrentDictionary<Guid, AsyncLazy<UserStatisticsModel>> UserStatisticsCache = new ConcurrentDictionary<Guid, AsyncLazy<UserStatisticsModel>>();

        public void InvalidateCache(Guid userId)
        {
            UserStatisticsCache.TryRemove(userId, out var _);
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
