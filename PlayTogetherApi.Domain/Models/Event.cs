using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlayTogetherApi.Domain
{
    public class Event
    {
        [Key]
        public Guid EventId { get; set; }

        [MaxLength(50)]
        [MinLength(5)]
        public string Title { get; set; }

        public Guid CreatedByUserId { get; set; }

        [ForeignKey("CreatedByUserId")]
        public User CreatedByUser { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}
