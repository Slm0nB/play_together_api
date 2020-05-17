using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.EntityFrameworkCore.Internal;
using PlayTogetherApi.Data;
using PlayTogetherApi.Web.Models;
using PlayTogetherApi.Web.Services;

namespace PlayTogetherApi.Services
{
    public class EventSearchObservable : CountingObservableBase<EventSearchUpdateModel>
    {
        readonly ObservablesService observablesService;
        readonly EventsQueryService query;
        private List<Data.Event> CurrentEvents; // todo: turn into a concurrent datastructure

        private IDisposable subscription1, subscription2, subscription3;

        public EventSearchObservable(ObservablesService observablesService, EventsQueryService query, List<Data.Event> initialEvents)
        {
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
                    if (eventSignup.UserId == query.UserId.Value)
                    {
                        // todo: if the signup is the subscriber or a friend, determine if this moves the event in or out of the collection

                    }
                    else if((query.IncludeByFriendsFilter || query.OnlyByFriendsFilter) && query.FriendIds?.Contains(eventSignup.UserId) == true)
                    {

                    }
                    else if(query.IncludeByUsersFilter?.Contains(eventSignup.UserId) == true)
                    {

                    }
                    else if (query.OnlyByUsersFilter?.Contains(eventSignup.UserId) == true)
                    {

                    }
                }
            });

            if (query.UserId.HasValue)
            {
                subscription3 = observablesService.UserRelationChangeStream.Subscribe(userRelationChanged =>
                {
                    if (query.IncludeByFriendsFilter || query.OnlyByFriendsFilter) // todo: joinedbyfriends filter when it gets added
                    {
                        // todo: determine if this updates the friendlist of the subscribing user (if this is relevant to the query!)
                        // todo: if the friendlist is updated, determine if this affects events in the collection or outside the collection (ie rerun the whole query

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

                                // todo: now we should run the query on all events created by or joing by the new friend
                            }
                            else if(!areFriends && query.FriendIds?.Contains(friendId) == true)
                            {
                                query.FriendIds.Remove(friendId);

                                // todo: now we should retun the query on the existing list of events
                            }
                        }
                    }

                });
            }

            return subject;
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
