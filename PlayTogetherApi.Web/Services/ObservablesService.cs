using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace PlayTogetherApi.Services
{
    public class ObservablesService
    {
        public ISubject<Domain.Event> GameEventStream = new ReplaySubject<Domain.Event>(1);

        public ISubject<Domain.UserEventSignup> UserEventSignupStream = new ReplaySubject<Domain.UserEventSignup>(1);
    }
}
