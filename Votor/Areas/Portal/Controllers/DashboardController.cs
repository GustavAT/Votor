using System;
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
        private readonly VotorContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        
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

        public async Task<IActionResult> ActivateEvent(Guid eventId)
        {
            var target = await GetEventById(eventId);

            if (CanActivate(target))
            {
                target.StartDate = DateTime.UtcNow;
                _context.SaveChanges();
            }

            return View("Dashboard", await InitEventListModel());
        }

        public async Task<IActionResult> FinishEvent(Guid eventId)
        {
            var target = await GetEventById(eventId);

            if (CanFinish(target))
            {
                target.EndDate = DateTime.UtcNow;
                _context.SaveChanges();
            }

            return View("Dashboard", await InitEventListModel());
        }
        
        [HttpPost]
        public async Task<IActionResult> CreateEvent(EventModel model)
        {
            var userId = await GetUserId();

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
                    eventId = newEvent.ID
                });
            }

            return View("Dashboard", await InitEventListModel());
        }

        #region helper

        /// <summary>
        /// Load all events of currently logged in user.
        /// </summary>
        /// <returns>All events of currently logged in user</returns>
        private async Task<EventListModel> InitEventListModel()
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = Util.ParseGuid(user?.Id);

            var allEvents = _context.Events
                .Where(x => x.UserID == userId)
                .Include(x => x.Options)
                .Include(x => x.Questions)
                .Include(x => x.Tokens)
                .AsNoTracking()
                .ToList();

            var source = new List<DashboardEventModel>();
            foreach (var record in allEvents)
            {
                source.Add(new DashboardEventModel
                {
                    Id = record.ID,
                    Name = record.Name,
                    StartDate = record.StartDate,
                    EndDate = record.EndDate,
                    CanActivate = CanActivate(record),
                    CanEdit = CanEdit(record),
                    CanFinish = CanFinish(record)
                });
            }
            
            var events = source.Where(x => !x.StartDate.HasValue).ToList();
            var active = source.Where(x => x.StartDate.HasValue && !x.EndDate.HasValue).ToList();
            var finished = source.Where(x => x.StartDate.HasValue && x.EndDate.HasValue).ToList();


            return new EventListModel
            {
                Events = events,
                Active = active,
                Finished = finished
            };
        }

        public static bool CanEdit(Event e)
        {
            return !e.StartDate.HasValue && !e.EndDate.HasValue; // not started
        }

        public static bool CanActivate(Event e)
        {
            // event started
            if (e.StartDate.HasValue) return false;

            // event finished
            if (e.EndDate.HasValue) return false;

            // no questions
            if (!e.Questions.Any()) return false;

            // no options
            if (!e.Options.Any()) return false;

            // public voting: ok
            if (e.IsPublic) return true;

            // no tokens and private
            if (!e.Tokens.Any()) return false;

            var firstOption = e.Options.FirstOrDefault();
            if (firstOption == null) return false;

            // more than one option: ok
            if (e.Options.Count != 1) return true;

            // only one option and restricted by token
            return !e.Tokens.All(x => x.OptionID.HasValue && x.OptionID.Value == firstOption.ID);
        }

        public static bool CanFinish(Event e)
        {
            return e.StartDate.HasValue && !e.EndDate.HasValue; // started but not finished
        }

        private async Task<Guid?> GetUserId()
        {
            var user = await _userManager.GetUserAsync(User);
            return Util.ParseGuid(user?.Id);
        }

        private async Task<Event> GetEventById(Guid id)
        {
            var userId = await GetUserId();
            return _context.Events.Where(x => x.ID == id && x.UserID == userId)
                .Include(x => x.Options)
                .Include(x => x.Questions)
                .Include(x => x.Tokens)
                .FirstOrDefault();
        }

        #endregion
    }

    public class EventListModel
    {
        public List<DashboardEventModel> Events { get; set; } = new List<DashboardEventModel>();
        public List<DashboardEventModel> Active { get; set; } = new List<DashboardEventModel>();
        public List<DashboardEventModel> Finished { get; set; } = new List<DashboardEventModel>();
    }

    public class DashboardEventModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool CanEdit { get; set; }
        public bool CanActivate { get; set; }
        public bool CanFinish { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}