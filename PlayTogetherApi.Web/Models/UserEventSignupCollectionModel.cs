using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Web.Models
{
    public class UserEventSignupCollectionModel
    {
        public IQueryable<UserEventSignup> TotalItemsQuery;
        public IQueryable<UserEventSignup> ItemsQuery;
    }
}
