using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace Votor.Areas.Portal.Models
{
    public class Event
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsPublic { get; set; }
        public string Password { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public Guid UserID { get; set; }

        public ICollection<Option> Options { get; set; }
        public ICollection<Question> Questions { get; set; }
        public ICollection<Token> Tokens { get; set; }
        public ICollection<Vote> Votes { get; set; }
    }
}
