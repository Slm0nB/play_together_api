using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GameCalendarApi.Domain
{
    public class User
    {
        [Key]
        public Guid UserId { get; set; }

        [MaxLength(50)]
        [MinLength(5)]
        public string DisplayName { get; set; }

        public string Email { get; set; }

        public string PasswordHash { get; set; }
    }
}
