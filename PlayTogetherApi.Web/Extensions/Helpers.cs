using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlayTogetherApi.Domain;
using PlayTogetherApi.Web.Models;

namespace PlayTogetherApi.Extensions
{
    static public class Helpers
    {
        public const UserRelationInternalStatus Relation_A_Mask = (UserRelationInternalStatus)0x00FF;
        public const UserRelationInternalStatus Relation_B_Mask = (UserRelationInternalStatus)0xFF00;
        public const UserRelationInternalStatus Relation_MutualFriends = UserRelationInternalStatus.A_Befriended | UserRelationInternalStatus.B_Befriended;

        static public UserRelationStatus GetStatusForUser(this UserRelation relation, Guid userId)
        {
            if (relation.UserAId != userId && relation.UserBId != userId)
            {
                throw new ArgumentException("UserId not found in the relation.");
            }

            var userFlags = relation.Status & (relation.UserAId == userId ? Relation_A_Mask : Relation_B_Mask );
            var relationFlags = relation.Status & (relation.UserAId == userId ? Relation_B_Mask : Relation_A_Mask );

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
                            return UserRelationStatus.Inviting;

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
    }
}
