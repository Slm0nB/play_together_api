using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Web.Models
{
    public class EventCollectionModel
    {
        public IQueryable<Event> TotalEventsQuery;
        public IQueryable<Event> EventsQuery;
    }
}
