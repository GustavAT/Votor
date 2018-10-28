using System;
using System.Collections.Generic;

namespace Votor.Areas.Portal.Models
{
    public class Token
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        
        public Guid EventID { get; set; }
        public Event Event { get; set; }

        public Guid? OptionID { get; set; }
        public Option Option { get; set; }
    }
}
