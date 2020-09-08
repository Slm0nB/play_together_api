using System;
using System.Linq;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Web.Models
{
    public class UserEventSignupCollectionModel
    {
        public IQueryable<UserEventSignup> TotalItemsQuery;
        public IQueryable<UserEventSignup> ItemsQuery;
    }
}
