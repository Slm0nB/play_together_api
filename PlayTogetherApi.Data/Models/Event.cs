using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlayTogetherApi.Data
{
    public class Event
    {
        [Key]
        public Guid EventId { get; set; } = Guid.NewGuid();

        [MaxLength(50)]
        [MinLength(5)]
        public string Title { get; set; }

        public Game Game { get; set; } = null;
        [ForeignKey("Game")]
        public Guid? GameId { get; set; } = null;

        public Guid CreatedByUserId { get; set; }

        [ForeignKey("CreatedByUserId")]
        public User CreatedByUser { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime EventDate { get; set; }
        public DateTime EventEndDate { get; set; }

        public bool FriendsOnly { get; set; }

        public string Description { get; set; }

        public ICollection<UserEventSignup> Signups { get; } = new List<UserEventSignup>();
    }
}
