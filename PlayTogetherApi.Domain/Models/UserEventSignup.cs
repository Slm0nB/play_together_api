using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlayTogetherApi.Domain
{
    public class UserEventSignup
    {
        public Event Event{ get; set; } = null;
        [ForeignKey("Event")]
        public Guid EventId { get; set; }

        public User User { get; set; } = null;
        [ForeignKey("User")]
        public Guid UserId { get; set; }

        public DateTime SignupDate { get; set; } = DateTime.Now;
    }
}
