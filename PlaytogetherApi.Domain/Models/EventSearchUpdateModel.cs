using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data = PlayTogetherApi.Data;

namespace PlayTogetherApi.Web.Models
{
    public class EventSearchUpdateModel
    {
        public List<Data.Event> Added;
        public List<Data.Event> Removed;
    }
}
