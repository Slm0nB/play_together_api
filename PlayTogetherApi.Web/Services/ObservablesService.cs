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
        public ISubject<Domain.Event> GameEventStream = new ReplaySubject<Domain.Event>(0);

        public ISubject<Domain.UserEventSignup> UserEventSignupStream = new ReplaySubject<Domain.UserEventSignup>(0);

        public ISubject<UserRelationExtModel> UserRelationStream = new ReplaySubject<UserRelationExtModel>(0);
    }
}
