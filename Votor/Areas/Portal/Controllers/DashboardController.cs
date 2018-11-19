using System.Collections.Generic;
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

        public async Task<IActionResult> Index()
        {
            return View("Dashboard", await InitEventListModel());
        }

        public async Task<IActionResult> Events()
        {
            var events = await InitEventListModel();
            return View("Events", events.Events);
        }

        public async Task<IActionResult> Active()
        {
            var events = await InitEventListModel();
            return View("Active", events.Active);
        }

        public async Task<IActionResult> Finished()
        {
            var events = await InitEventListModel();
            return View("Finished", events.Finished);
        }

        [HttpPost]
        public async Task<IActionResult> CreateEvent(EventModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = Util.ParseGuid(user?.Id);

            if (ModelState.IsValid && userId.HasValue)
            {
                var newEvent = new Event
                {
                    Name = model.EventName,
                    IsPublic = true,
                    UserID = userId.Value
                };

                _context.Events.Add(newEvent);
                _context.SaveChanges();

                return RedirectToAction("Edit", "Event", new
                {
                    id = newEvent.ID
                });
            }

            return View("Dashboard", await InitEventListModel());
        }

        /// <summary>
        /// Load all events of currently logged in user.
        /// </summary>
        /// <returns>All events of currently logged in user</returns>
        private async Task<EventListModel> InitEventListModel()
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = Util.ParseGuid(user?.Id);

            var events = _context.Events
                .Where(x => x.UserID == userId)
                .AsNoTracking().ToList();
            
            return new EventListModel
            {
                Events = events.Where(x => !x.StartDate.HasValue).ToList(),
                Active = events.Where(x => x.StartDate.HasValue && !x.EndDate.HasValue).ToList(),
                Finished = events.Where(x => x.StartDate.HasValue && x.EndDate.HasValue).ToList()
            };
        }
    }

    public class EventListModel
    {
        public List<Event> Events { get; set; } = new List<Event>();
        public List<Event> Active { get; set; } = new List<Event>();
        public List<Event> Finished { get; set; } = new List<Event>();
    }
}