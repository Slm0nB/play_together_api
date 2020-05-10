using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlayTogetherApi.Data
{
    public class PlaceholderImage
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int ImageId { get; set; }

        [MaxLength(100)]
        public string ImagePath { get; set; }

        public PlaceholderImageCategory Category { get; set; }
    }
}
