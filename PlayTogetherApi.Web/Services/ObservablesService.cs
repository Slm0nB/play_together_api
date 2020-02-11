using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        
    }
}
