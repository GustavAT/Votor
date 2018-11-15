using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Votor.Areas.Portal.Data;
using Votor.Areas.Portal.Models;

namespace Votor.Areas.Portal.Controllers
{
    [Area("Portal")]
    [Authorize]
    public class DashboardController : Controller
    {
        private VotorContext _context;
        private UserManager<IdentityUser> _userManager;

        public DashboardController(VotorContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View("Dashboard", new EventListModel
            {
                Events = LoadEvents()
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateEvent(CreateEventModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = Util.ParseGuid(user?.Id);

            if (ModelState.IsValid && userId.HasValue)
            {
                var newEvent = new Event
                {
                    Name = model.Name,
                    IsPublic = true,
                    UserID = userId.Value
                };

                _context.Events.Add(newEvent);
                _context.SaveChanges();

                return RedirectToAction("EditEvent", "Event", new
                {
                    id = newEvent.ID
                });
            }

            return View("Dashboard", new EventListModel
            {
                Events = LoadEvents()
            });
        }

        private List<Event> LoadEvents()
        {
            return _context.Events.AsNoTracking().ToList();
        }
    }

    public class EventListModel
    {
        public List<Event> Events { get; set; } = new List<Event>();
    }

    public class CreateEventModel
    {
        [Required(ErrorMessage = "The {0} field is required.")]
        public string Name { get; set; }
        public string Test { get; set; }
    }
}