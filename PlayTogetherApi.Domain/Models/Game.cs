using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlayTogetherApi.Domain
{
    public class Game
    {
        [Key]
        public Guid GaneId { get; set; } = Guid.NewGuid();

        [MaxLength(50)]
        [MinLength(5)]
        public string Title { get; set; }

        [MaxLength(100)]
        public string ImagePath { get; set; }
    }
}
