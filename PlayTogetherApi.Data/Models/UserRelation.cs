using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlayTogetherApi.Data
{
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
