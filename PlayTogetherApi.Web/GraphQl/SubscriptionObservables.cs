using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace PlayTogetherApi.Web.GraphQl
{
    public class SubscriptionObservables
    {
        public ISubject<Domain.Event> EventStream = new ReplaySubject<Domain.Event>(1);

        public ISubject<Domain.UserEventSignup> EventSignupStream = new ReplaySubject<Domain.UserEventSignup>(1);
    }
}
