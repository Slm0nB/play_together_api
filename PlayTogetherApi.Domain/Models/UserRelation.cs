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
        public User FromUser { get; set; } = null;
        [ForeignKey("User")]
        public Guid FromUserId { get; set; }

        public User ToUser { get; set; } = null;
        [ForeignKey("User")]
        public Guid ToUserId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public UserRelationStatus Status { get; set; }
    }
}
