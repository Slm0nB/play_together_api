﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PlayTogetherApi.Data
{
    public class User
    {
        [Key]
        public Guid UserId { get; set; }

        [MaxLength(50)]
        [MinLength(5)]
        public string DisplayName { get; set; }

        public int DisplayId { get; set; }

        public string Email { get; set; }

        public string PasswordHash { get; set; }

        public ICollection<UserEventSignup> Signups { get; set; } = new List<UserEventSignup>();

        public string AvatarFilename { get; set; }

        public TimeSpan? UtcOffset { get; set; }

        public string DeviceToken { get; set; }

        public bool SoftDelete { get; set; }
    }
}
