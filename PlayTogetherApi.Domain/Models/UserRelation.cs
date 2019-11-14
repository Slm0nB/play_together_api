using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlayTogetherApi.Domain
{
    //public enum UserRelationStatus
    //{
    //    Invited = 1,
    //    Accepted = 2,
    //    Rejected = 3,
    //    Blocked = 4
    //}

    public enum UserRelationStatus
    {
        A_Invited_B = 1,
        B_Invited_A = 2,
        Friends = 3,
        A_Rejected = 4,
        B_Rejected = 5,
        A_Blocked_B = 6,
        B_Blocked_A = 7,
        Both_Blocked = 8
    }

    public class UserRelation
    {
        public User UserA { get; set; } = null;
        [ForeignKey("User")]
        public Guid UserAId { get; set; }

        public User UserB { get; set; } = null;
        [ForeignKey("User")]
        public Guid UserBId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public UserRelationStatus Status { get; set; }
    }
}
