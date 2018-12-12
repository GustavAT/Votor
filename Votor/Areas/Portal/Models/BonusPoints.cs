using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Votor.Areas.Portal.Models
{
    public class BonusPoints
    {
        public Guid ID { get; set; }

        public double Points { get; set; }

        public string Reason { get; set; }

        public Guid? OptionID { get; set; }
        public Option Option { get; set; }

        public Guid? QuestionID { get; set; }
        public Question Question { get; set; }

        public Guid EventID { get; set; }
        public Event Event { get; set; }
    }
}
