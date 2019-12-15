using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlayTogetherApi.Data
{
    [Flags]
    public enum UserRelationInternalStatus
    {
        None = 0,
        A_Invited = 1<<0,
        A_Befriended = 1<<1,
        A_Rejected = 1<<2,
        A_Blocked = 1<<3,

        B_Invited = 1 << 8,
        B_Befriended = 1 << 9,
        B_Rejected = 1 << 10,
        B_Blocked = 1 << 11
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

        public UserRelationInternalStatus Status { get; set; }
    }
}
