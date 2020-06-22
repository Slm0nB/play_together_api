using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using PlayTogetherApi.Models;

namespace PlayTogetherApi.Services
{
    public class ObservablesService
    {
        // don't subscribe directly to these; only use them to raise the next event, and then add helper methods here to subscribe to them.
        public ISubject<EventChangedModel> GameEventStream = new ReplaySubject<EventChangedModel>(0);
        public ISubject<Data.UserEventSignup> UserEventSignupStream = new ReplaySubject<Data.UserEventSignup>(0);
        public ISubject<UserRelationChangedModel> UserRelationChangeStream = new ReplaySubject<UserRelationChangedModel>(0);
        public ISubject<UserChangedSubscriptionModel> UserChangeStream = new ReplaySubject<UserChangedSubscriptionModel>(0);
        public ConcurrentDictionary<Guid, ISubject<UserStatisticsModel>> UserStatisticsStreams = new ConcurrentDictionary<Guid, ISubject<UserStatisticsModel>>();

        public IObservable<EventChangedModel> GetGameEventStream()
            => GameEventStream
                .AsObservable();

        public IObservable<Data.UserEventSignup> GetUserEventSignupStream()
            => UserEventSignupStream
                .AsObservable();

        public IObservable<UserChangedSubscriptionModel> GetUserChangeStream()
            => UserChangeStream
                .AsObservable();

        public IObservable<UserChangedSubscriptionModel> GetUserChangeStream(Guid userId, bool excludeIfNotUserOrFriend = false, bool excludeIfCausedByUser = false)
            => UserChangeStream
                .Where(model => !excludeIfNotUserOrFriend || model.FriendsOfChangingUser.Any(rel => rel.UserAId == userId || rel.UserBId == userId))
                .Where(model => !excludeIfCausedByUser || model.ChangingUser.UserId != userId)
                .AsObservable();

        public ISubject<UserStatisticsModel> GetUserStatisticsStream(Guid userId, bool createIfMissing = true)
            => createIfMissing
                ? UserStatisticsStreams.GetOrAdd(userId, _ => new CountingSubject<UserStatisticsModel>(() => { UserStatisticsStreams.TryRemove(userId, out var __); }))
                : UserStatisticsStreams.TryGetValue(userId, out var existingSubject)
                    ? existingSubject
                    : null;

        public IObservable<EventChangedModel> GetEventsStream(Guid? userId)
            => GameEventStream
                .Where(eventExt => !eventExt.Event.FriendsOnly || eventExt.Event.CreatedByUserId == userId || (userId.HasValue && eventExt.FriendsOfChangingUser != null && eventExt.FriendsOfChangingUser.Any(nn => nn.UserAId == userId ||nn.UserBId == userId)))
                .Where(eventExt => !eventExt.RecipientUserId.HasValue || eventExt.RecipientUserId == userId)
                .AsObservable();

        public IObservable<UserRelationChangedModel> GetUserRelationChangeStream()
            => UserRelationChangeStream
                .AsObservable();

        public IObservable<UserRelationChangedExtModel> GetExtUserRelationChangeStream(Guid userId, bool excludeFromUser = false)
            => UserRelationChangeStream
                .AsObservable()
                .Where(model => model.Relation.UserAId == userId || model.Relation.UserBId == userId)
                .Where(model => !excludeFromUser || model.ActiveUser.UserId != userId)
                .Select(model => new UserRelationChangedExtModel(model, userId));
    }
}
