using System;

namespace PlayTogetherApi.Models
{
    public enum SingleUserRelationStatus
    {
        NoRelation = 0,
        IsInvited = 1,
        HasInvited = 2,
        Friends = 3,
        IsRejected = 4,
        HasRejected = 5,
        IsBlocked = 6,
        HasBlocked = 7,
        BothBlocked = 8
    }

    public enum SingleUserRelationStatusChange
    {
        Remove = 0,
        Invite = 1,
        Reject = 2,
        Accept = 3,
        Block = 4
    }
}
