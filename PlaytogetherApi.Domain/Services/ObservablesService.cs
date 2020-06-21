using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using PlayTogetherApi.Web.Models;

namespace PlayTogetherApi.Services
{
    public class ObservablesService
    {
        // todo: stop subscribing directly to any of these, and instead only use methods in this class that create and return a subscription
        public ISubject<EventChangedModel> GameEventStream = new ReplaySubject<EventChangedModel>(0);
        public ISubject<Data.UserEventSignup> UserEventSignupStream = new ReplaySubject<Data.UserEventSignup>(0);
        public ISubject<UserRelationChangedModel> UserRelationChangeStream = new ReplaySubject<UserRelationChangedModel>(0);
        public ISubject<UserChangedSubscriptionModel> UserChangeStream = new ReplaySubject<UserChangedSubscriptionModel>(0);
        public ConcurrentDictionary<Guid, ISubject<UserStatisticsModel>> UserStatisticsStreams = new ConcurrentDictionary<Guid, ISubject<UserStatisticsModel>>();
        
        public ISubject<UserStatisticsModel> GetUserStatisticsStream(Guid userId, bool createIfMissing = true)
            => createIfMissing
                ? UserStatisticsStreams.GetOrAdd(userId, _ => new CountingSubject<UserStatisticsModel>(() => { UserStatisticsStreams.TryRemove(userId, out var __); }))
                : UserStatisticsStreams.TryGetValue(userId, out var existingSubject)
                    ? existingSubject
                    : null;

        public IObservable<EventChangedModel> GetEventsSubscriptioon(Guid? userId)
            => GameEventStream
                .Where(eventExt => !eventExt.Event.FriendsOnly || eventExt.Event.CreatedByUserId == userId || (userId.HasValue && eventExt.FriendsOfChangingUser != null && eventExt.FriendsOfChangingUser.Any(nn => nn.UserAId == userId ||nn.UserBId == userId)))
                .Where(eventExt => !eventExt.RecipientUserId.HasValue || eventExt.RecipientUserId == userId)
                .AsObservable();

        public IObservable<UserRelationChangedExtModel> GetRelationChangedSubscription(Guid callingUserId, bool excludeChangesFromCaller)
        {
            IObservable<UserRelationChangedModel> observable = this.UserRelationChangeStream
                .Where(rel => rel.Relation.UserAId == callingUserId || rel.Relation.UserBId == callingUserId);

            if (excludeChangesFromCaller)
            {
                observable = observable.Where(rel => rel.ActiveUser.UserId != callingUserId);
            }

            return observable
                .Select(n => new UserRelationChangedExtModel(n, callingUserId))
                .AsObservable();
        }
    }
}
