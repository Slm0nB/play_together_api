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
        public ISubject<EventExtModel> GameEventStream = new ReplaySubject<EventExtModel>(0);

        public ISubject<Data.UserEventSignup> UserEventSignupStream = new ReplaySubject<Data.UserEventSignup>(0);

        public ISubject<UserRelationExtModel> UserRelationStream = new ReplaySubject<UserRelationExtModel>(0);
    }
}
