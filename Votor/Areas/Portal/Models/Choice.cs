using System;

namespace Votor.Areas.Portal.Models
{
    public class Choice
    {
        public Guid ID { get; set; }
        
        public Guid QuestionID { get; set; }
        public Question Question { get; set; }

        public Guid? OptionID { get; set; }
        public Option Option { get; set; }

        public Guid? VoteID { get; set; }
        public Vote Vote { get; set; }
    }
}
