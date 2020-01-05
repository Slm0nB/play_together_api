using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlayTogetherApi.Web.Models
{
    public enum UserRelationStatusAction
    {
        Invite,
        Accept,
        Reject,
        Block,
        Remove,
    }
}
