using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlayTogetherApi.Domain
{
    public class BuiltinAvatar
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int AvatarId { get; set; }

        [MaxLength(100)]
        public string ImagePath { get; set; }
    }
}
