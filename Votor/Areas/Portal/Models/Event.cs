using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Votor.Areas.Portal.Models
{
    public class Event
    {
        public Guid ID { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
        
        [DefaultValue(false)]
        public bool IsPublic { get; set; }

        [DefaultValue(false)]
        public bool ShowOverallWinner { get; set; }

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
