using System;
using System.ComponentModel.DataAnnotations;

namespace Votor.Areas.Portal.Models
{
    public class Option
    {
        public Guid ID { get; set; }

        public string Name { get; set; }

        public Guid EventID { get; set; }
        public Event Event { get; set; }
    }
}
