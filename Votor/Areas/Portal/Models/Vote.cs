using System;

namespace Votor.Areas.Portal.Models
{
    public class Vote
    {
        public Guid ID { get; set; }
        public bool IsCompleted { get; set; }

        public Guid EventID { get; set; }
        public Event Event { get; set; }

        public Guid? QuestionID { get; set; }
        public Question Question { get; set; }

        public Guid? OptionID { get; set; }
        public Option Option { get; set; }

        public Guid? TokenID { get; set; }
        public Token Token { get; set; }

        public Guid? CookieID { get; set; }
    }
}
