using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlayTogetherApi.Domain;

namespace PlayTogetherApi.Web.Models
{
    public class UserRelationCollectionModel
    {
        public Guid UserId;
        public IQueryable<UserRelation> TotalItemsQuery;
        public IQueryable<UserRelation> ItemsQuery;
    }
}
