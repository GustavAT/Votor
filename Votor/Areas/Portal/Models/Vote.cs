using System;
using System.Collections.Generic;

namespace Votor.Areas.Portal.Models
{
    public class Vote
    {
        public Guid ID { get; set; }
        
        public bool IsCompleted { get; set; }

        public Guid EventID { get; set; }
        public Event Event { get; set; }

        public ICollection<Choice> Choices { get; set; }

        public Guid? TokenID { get; set; }
        public Token Token { get; set; }

        public Guid? CookieID { get; set; }
    }
}
