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
        public ISubject<EventChangedModel> GameEventStream = new ReplaySubject<EventChangedModel>(0);

        public ISubject<Data.UserEventSignup> UserEventSignupStream = new ReplaySubject<Data.UserEventSignup>(0);

        public ISubject<UserRelationChangedModel> UserRelationChangeStream = new ReplaySubject<UserRelationChangedModel>(0);

        public ISubject<UserChangedSubscriptionModel> UserChangeStream = new ReplaySubject<UserChangedSubscriptionModel>(0);

        public ConcurrentDictionary<Guid, ISubject<UserStatisticsModel>> UserStatisticsStreams = new ConcurrentDictionary<Guid, ISubject<UserStatisticsModel>>();
        
        // todo: it would be nice to wrap this in a custom IDisposable that removes it from the collection when there are no subscribers!
        public ISubject<UserStatisticsModel> GetUserStatisticsStream(Guid userId, bool createIfMissing = true)
            => createIfMissing
                ? UserStatisticsStreams.GetOrAdd(userId, _ => new ReplaySubject<UserStatisticsModel>(0))
                : UserStatisticsStreams.TryGetValue(userId, out var existingSubject)
                    ? existingSubject
                    : null;
    }
}
