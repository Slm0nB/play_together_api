using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using PlayTogetherApi.Data;
using PlayTogetherApi.Web.Models;
using PlayTogetherApi.Web.Services;

namespace PlayTogetherApi.Services
{
    public class EventSearchObservable : CountingObservableBase<EventSearchUpdateModel>
    {
        readonly IServiceProvider serviceProvider;
        readonly ObservablesService observablesService;
        readonly EventsQueryService query;
        private List<Data.Event> CurrentEvents; // todo: turn into a concurrent datastructure

        private IDisposable subscription1, subscription2, subscription3;

        public EventSearchObservable(IServiceProvider serviceProvider, ObservablesService observablesService, EventsQueryService query, List<Data.Event> initialEvents)
        {
            this.serviceProvider = serviceProvider;
            this.observablesService = observablesService;
            this.query = query;
            this.CurrentEvents = initialEvents;
        }

        protected override ISubject<EventSearchUpdateModel> Setup()
        {
            var subject = new Subject<EventSearchUpdateModel>();

            subscription1 = observablesService.GameEventStream.Subscribe(eventChanged =>
            {
                if(eventChanged.Action == EventAction.Deleted)
                {
                    Update(removed: new List<Event>() { eventChanged.Event });
                }
                else
                {
                    var isMatch = query.Process(new[] { eventChanged.Event }.AsQueryable()).Any();
                    if(isMatch)
                    {
                        Update(added: new List<Event>() { eventChanged.Event });
                    }
                    else
                    {
                        Update(removed: new List<Event>() { eventChanged.Event });
                    }
                }
            });

            subscription2 = observablesService.UserEventSignupStream.Subscribe(eventSignup =>
            {
                if (query.UserId.HasValue)
                {
                    if (eventSignup.UserId == query.UserId.Value || query.FriendIds?.Contains(eventSignup.UserId) == true) // update if it was the user that joined or left.
                    {
                        if (eventSignup.Status == UserEventStatus.Cancelled)
                        {
                            RerunQuery();
                        }
                        else
                        {
                            FilterAndUpdate(new[] { eventSignup.Event });
                        }
                    }
                }
            });

            if (query.UserId.HasValue)
            {
                subscription3 = observablesService.UserRelationChangeStream.Subscribe(userRelationChanged =>
                {
                    if (userRelationChanged.Relation.UserAId == query.UserId || userRelationChanged.Relation.UserBId == query.UserId)
                    {
                        var areFriends = userRelationChanged.Relation.Status == (UserRelationInternalStatus.A_Befriended | UserRelationInternalStatus.B_Befriended);
                        var friendId = userRelationChanged.Relation.UserAId == query.UserId
                            ? userRelationChanged.Relation.UserBId
                            : userRelationChanged.Relation.UserAId;

                        if(areFriends && query.FriendIds?.Contains(friendId) != true)
                        {
                            query.FriendIds = query.FriendIds ?? new List<Guid>();
                            query.FriendIds.Add(friendId);

                            GetEventsCreatedByorJoinedByUserAsync(friendId)
                                .ContinueWith(eventsTask =>
                                {
                                    FilterAndUpdate(eventsTask.Result);
                                });
                        }
                        else if(!areFriends && query.FriendIds?.Contains(friendId) == true)
                        {
                            query.FriendIds.Remove(friendId);
                            RerunQuery();
                        }
                    }
                });
            }

            return subject;
        }

        async Task<List<Event>> GetEventsCreatedByorJoinedByUserAsync(Guid userId)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetService<PlayTogetherDbContext>();

                bool includeJoinedEvents = true; // todo: skip this in the cases where we can ignore them

                var eventsQuery = context.Events.Where(n => n.CreatedByUserId == userId || (includeJoinedEvents && n.Signups.Any(nn => nn.UserId == userId)));

                eventsQuery = eventsQuery.Include(n => n.Signups);
                eventsQuery = query.ProcessDates(eventsQuery);

                var events = await eventsQuery.ToListAsync();
                return events;
            }
        }

        void RerunQuery()
        {
            var updatedEvents = query.Process(CurrentEvents.AsQueryable()).ToList();
            var removed = CurrentEvents.Except(updatedEvents).ToList();
            if(removed.Any())
            {
                Update(removed: removed);
            }
        }

        void FilterAndUpdate(IEnumerable<Event> events)
        {
            var addedEvents = query.Process(events.AsQueryable()).ToList();
            var removedEvents = events.Except(addedEvents).ToList();
            Update(added: addedEvents, removed: removedEvents);
        }

        void Update(List<Data.Event> added = null, List<Data.Event> removed = null)
        {
            if (added != null)
            {
                added = added.Where(n => !CurrentEvents.Any(nn => nn.EventId == n.EventId)).ToList();
                CurrentEvents.AddRange(added);
            }

            if (removed != null) {
                removed = CurrentEvents.Where(n => removed.Any(nn => nn.EventId == n.EventId)).ToList();
                CurrentEvents = CurrentEvents.Except(removed).ToList();
            }

            if (added?.Any() == true || removed?.Any() == true)
            {
                InternalSubject.OnNext(
                    new EventSearchUpdateModel
                    {
                        Added = added,
                        Removed = removed
                    });
            }
        }

        protected override void Teardown()
        {
            subscription1?.Dispose();
            subscription1 = null;

            subscription2?.Dispose();
            subscription2 = null;

            subscription3?.Dispose();
            subscription3 = null;
        }
    }

}
