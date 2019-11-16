using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlayTogetherApi.Web.Models
{
    public enum UserRelationStatus
    {
        None,
        Invited,
        Inviting,
        Friends,
        Rejected,
        Rejecting,
        Blocked,
        Blocking
    }
}
