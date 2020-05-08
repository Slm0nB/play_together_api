using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using PlayTogetherApi.Data;
using PlayTogetherApi.Web.Models;
using PlayTogetherApi.Web.Services;

namespace PlayTogetherApi.Services
{
    public class EventSearchObservable : CountingObservableBase<Data.Event>
    {
        readonly ObservablesService observablesService;
        readonly EventsQueryService query;

        private IDisposable subscription1, subscription2, subscription3;

        public EventSearchObservable(ObservablesService observablesService, EventsQueryService query)
        {
            this.observablesService = observablesService;
            this.query = query;

            // todo: build initial query
        }


        protected override ISubject<Event> Setup()
        {
            var subject = new Subject<Event>();

            // todo: set up all the subscriptions

            subscription1 = observablesService.GameEventStream.Subscribe(eventChanged =>
            {
                // todo: determine if new events should be added to the colleciton
                // todo: remove deleted events if they are in the collection
                // todo: if "friendsonly" changed, determine if that moves the event in or out of the collection

            });
            subscription2 = observablesService.UserEventSignupStream.Subscribe(eventSignup =>
            {
                // todo: if the signup is the subscriber or a friend, determine if this moves the event in or out of the collection

            });
            subscription3 = observablesService.UserRelationChangeStream.Subscribe(userRelationChanged =>
            {
                // todo: determine if this updates the friendlist of the subscribing user (if this is relevant to the query!)

                // todo: if the friendlist is updated, determine if this affects events in the collection

            });

            return subject;
        }

        protected override void Teardown(ISubject<Event> subject)
        {
            subscription1?.Dispose();
            subscription1 = null;

            subscription2?.Dispose();
            subscription2 = null;

            subscription3?.Dispose();
            subscription3 = null;

            // todo: remove this from the ObservablesService ?
        }
    }

}
