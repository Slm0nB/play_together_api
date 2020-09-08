using System;
using System.Linq;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Web.Models
{
    public class UserRelationCollectionModel
    {
        public Guid UserId;
        public IQueryable<UserRelation> TotalItemsQuery;
        public IQueryable<UserRelation> ItemsQuery;
    }
}
