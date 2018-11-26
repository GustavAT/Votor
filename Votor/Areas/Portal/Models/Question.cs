using System;

namespace Votor.Areas.Portal.Models
{
    public class Question
    {
        public Guid ID { get; set; }

        public string Text { get; set; }
        
        public Guid EventID { get; set; }
        public Event Event { get; set; }
    }
}
