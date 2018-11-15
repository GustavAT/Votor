using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Votor.Areas.Portal.Data;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Votor.Areas.Portal.Controllers
{
    [Area("Portal")]
    [Authorize]
    public class EventController : Controller
    {
        private VotorContext _context;
        
        public EventController(VotorContext context)
        {
            _context = context;
        }

        public IActionResult EditEvent(Guid id)
        {
            var editEvent = _context.Events.Where(x => x.ID == id).AsNoTracking().FirstOrDefault();

            if (editEvent == null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View("Edit", new EditEventModel
            {
                Name = editEvent.Name
            });
        }
    }

    public class EditEventModel
    {
        [Required]
        public string Name { get; set; }
    }
}
