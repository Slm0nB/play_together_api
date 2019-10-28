using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlayTogetherApi.Domain
{
    public enum UserRelationStatus
    {
        Invited = 1,
        Accepted = 2,
        Rejected = 3,
        Blocked = 4
    }

    public class UserRelation
    {
        public User FirstUser { get; set; } = null;
        [ForeignKey("User")]
        public Guid FirstUserId { get; set; }

        public User SecondUser { get; set; } = null;
        [ForeignKey("User")]
        public Guid SecondUserId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public UserRelationStatus Status { get; set; }
    }
}
