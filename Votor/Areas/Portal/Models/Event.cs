using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Votor.Areas.Portal.Models
{
    public class Event
    {
        public Guid ID { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        public string Name { get; set; }

        [StringLength(200, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        public string Description { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        public bool IsPublic { get; set; }

        public string Password { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? StartDate { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime? EndDate { get; set; }

        public Guid UserID { get; set; }

        public ICollection<Option> Options { get; set; }
        public ICollection<Question> Questions { get; set; }
        public ICollection<Token> Tokens { get; set; }
        public ICollection<Vote> Votes { get; set; }
    }
}
