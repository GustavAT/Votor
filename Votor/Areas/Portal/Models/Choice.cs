using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Votor.Areas.Portal.Models
{
    public class Choice
    {
        public Guid ID { get; set; }

        [ForeignKey("Vote")]
        public Guid VoteID { get; set; }
        public Vote Vote { get; set; }

        public Guid QuestionID { get; set; }
        public Question Question { get; set; }

        public Guid OptionID { get; set; }
        public Option Option { get; set; }
    }
}
