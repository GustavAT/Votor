using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Votor.Areas.Portal.Models
{
    public class Token
    {
        public Guid ID { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        public string Name { get; set; }
        
        public Guid EventID { get; set; }
        public Event Event { get; set; }

        public Guid? OptionID { get; set; }
        public Option Option { get; set; }
    }
}
