using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlayTogetherApi.Data;
using PlayTogetherApi.Web.Models;

namespace PlayTogetherApi.Services
{
    public class FriendLogicService
    {
        public const UserRelationInternalStatus Relation_A_Mask = (UserRelationInternalStatus)0x00FF;
        public const UserRelationInternalStatus Relation_B_Mask = (UserRelationInternalStatus)0xFF00;
        public const UserRelationInternalStatus Relation_MutualFriends = UserRelationInternalStatus.A_Befriended | UserRelationInternalStatus.B_Befriended;

        public void ExtractStatuses(UserRelation relation, Guid userId, out UserRelationInternalStatus userFlags, out UserRelationInternalStatus relationFlags)
        {
            userFlags = relation.Status & Relation_A_Mask;
            relationFlags = relation.Status & Relation_B_Mask;
            if (relation.UserBId == userId)
            {
                userFlags = (UserRelationInternalStatus)((int)(relation.Status & Relation_B_Mask) >> 8);
                relationFlags = (UserRelationInternalStatus)((int)(relation.Status & Relation_A_Mask) << 8);
            }
        }

        public UserRelationStatus GetStatusForUser(UserRelation relation, Guid userId)
        {
            if (relation.UserAId != userId && relation.UserBId != userId)
            {
                throw new ArgumentException("UserId not found in the relation.");
            }

            ExtractStatuses(relation, userId, out var userFlags, out var relationFlags);

            switch (userFlags)
            {
                case UserRelationInternalStatus.None:
                    switch (relationFlags)
                    {
                        case UserRelationInternalStatus.B_Invited:
                            return UserRelationStatus.Invited;
                    }
                    break;

                case UserRelationInternalStatus.A_Invited:
                case UserRelationInternalStatus.A_Befriended:
                    switch (relationFlags)
                    {
                        case UserRelationInternalStatus.None:
                            // if the other guy removes his "befriended" state, our own state goes to "none"
                            return userFlags == UserRelationInternalStatus.A_Befriended
                                ? UserRelationStatus.None
                                : UserRelationStatus.Inviting;

                        case UserRelationInternalStatus.B_Invited:
                        case UserRelationInternalStatus.B_Befriended:
                            return UserRelationStatus.Friends;

                        case UserRelationInternalStatus.B_Blocked:
                            return UserRelationStatus.Blocked;

                        case UserRelationInternalStatus.B_Rejected:
                            return UserRelationStatus.Rejected;
                    }
                    break;

                case UserRelationInternalStatus.A_Blocked:
                    return UserRelationStatus.Blocking;

                case UserRelationInternalStatus.A_Rejected:
                    switch (relationFlags)
                    {
                        case UserRelationInternalStatus.None:
                        case UserRelationInternalStatus.B_Invited:
                        case UserRelationInternalStatus.B_Befriended:
                        case UserRelationInternalStatus.B_Rejected:
                            return UserRelationStatus.Rejecting;

                        case UserRelationInternalStatus.B_Blocked:
                            return UserRelationStatus.Blocked;
                    }
                    break;
            }

            if (relationFlags == UserRelationInternalStatus.B_Invited)
            {
                return UserRelationStatus.Invited;
            }

            return UserRelationStatus.None;
        }

        public UserRelationInternalStatus GetUpdatedStatus(UserRelation relation, Guid callingUserId, UserRelationStatusChange statusChange)
        {
            if (relation.UserAId == callingUserId)
            {
                switch (statusChange)
                {
                    case UserRelationStatusChange.Invite:
                        if ((int)(relation.Status & (UserRelationInternalStatus.B_Invited | UserRelationInternalStatus.B_Befriended)) != 0)
                        {
                            return Relation_MutualFriends;
                        }
                        else
                        {
                            return (relation.Status & Relation_B_Mask) | UserRelationInternalStatus.A_Invited;
                        }

                    case UserRelationStatusChange.Accept:
                        if ((relation.Status & Relation_B_Mask) == UserRelationInternalStatus.B_Invited)
                        {
                            return Relation_MutualFriends;
                        }
                        else
                        {
                            return (relation.Status & Relation_B_Mask) | UserRelationInternalStatus.A_Befriended;
                        }

                    case UserRelationStatusChange.Reject:
                        return (relation.Status & Relation_B_Mask) | UserRelationInternalStatus.A_Rejected;

                    case UserRelationStatusChange.Block:
                        return (relation.Status & Relation_B_Mask) | UserRelationInternalStatus.A_Blocked;

                    case UserRelationStatusChange.Remove:
                        return relation.Status & Relation_B_Mask;
                }
            }
            else
            {
                switch (statusChange)
                {
                    case UserRelationStatusChange.Invite:
                        if ((int)(relation.Status & (UserRelationInternalStatus.A_Invited | UserRelationInternalStatus.A_Befriended)) != 0)
                        {
                            return Relation_MutualFriends;
                        }
                        else
                        {
                            return (relation.Status & Relation_A_Mask) | UserRelationInternalStatus.B_Invited;
                        }

                    case UserRelationStatusChange.Accept:
                        if ((relation.Status & Relation_A_Mask) == UserRelationInternalStatus.A_Invited)
                        {
                            return Relation_MutualFriends;
                        }
                        else
                        {
                            return (relation.Status & Relation_A_Mask) | UserRelationInternalStatus.B_Befriended;
                        }

                    case UserRelationStatusChange.Reject:
                        return (relation.Status & Relation_A_Mask) | UserRelationInternalStatus.B_Rejected;

                    case UserRelationStatusChange.Block:
                        return (relation.Status & Relation_A_Mask) | UserRelationInternalStatus.B_Blocked;

                    case UserRelationStatusChange.Remove:
                        return relation.Status & Relation_A_Mask;
                }
            }
            // this should never happen
            return relation.Status;
        }
    }
}
