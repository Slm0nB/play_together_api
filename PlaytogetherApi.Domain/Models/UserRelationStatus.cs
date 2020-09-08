using System;

namespace PlayTogetherApi.Models
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
