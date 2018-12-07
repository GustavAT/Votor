using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Votor.Areas.Portal.Data;
using Votor.Areas.Portal.Models;
using Votor.Areas.Voting.Controllers;

namespace Votor.Areas.Portal.Controllers
{
    [Area("Portal")]
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly VotorContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IStringLocalizer<SharedResources> _localizer;
        
        public DashboardController(VotorContext context, UserManager<IdentityUser> userManager,
            IStringLocalizer<SharedResources> localizer)
        {
            _context = context;
            _userManager = userManager;
            _localizer = localizer;
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
                target.EndDate = null;
                _context.SaveChanges();
            }

            return RedirectToAction("Details", "Event", new
            {
                eventId = target.ID
            });
        }

        public async Task<IActionResult> FinishEvent(Guid eventId)
        {
            var target = await GetEventById(eventId);

            if (CanFinish(target))
            {
                target.EndDate = DateTime.UtcNow;

                //var votes = _context.Votes.Where(x => x.EventID == eventId && !x.IsCompleted);
                //var choices = votes.SelectMany(x => x.Choices);

                //_context.Choices.RemoveRange(choices);
                //_context.Votes.RemoveRange(votes);
                _context.SaveChanges();
            }

            return RedirectToAction("Index", "Dashboard");
        }

        public async Task<IActionResult> Reset(Guid eventId)
        {
            var target = await GetEventById(eventId);

            if (target == null) return RedirectToAction("Index", "Dashboard");

            _context.RemoveRange(target.Votes.SelectMany(x => x.Choices));
            _context.RemoveRange(target.Votes);

            target.StartDate = target.EndDate = null;

            _context.SaveChanges();

            return RedirectToAction("Index", "Dashboard");
        }
        
        public async Task<IActionResult> Create(CloneEventModel model)
        {
            var userId = await GetUserId();

            // new event, no template
            if (!model.SelectedEvent.HasValue && userId.HasValue)
            {
                var newEvent = new Event
                {
                    Name = _localizer["New Event"],
                    IsPublic = true,
                    ShowOverallWinner = true,
                    UserID = userId.Value
                };

                _context.Events.Add(newEvent);
                _context.SaveChanges();

                return RedirectToAction("Edit", "Event", new
                {
                    eventId = newEvent.ID
                });
            }

            var targetEvent = _context.Events
                .Where(x => x.UserID == userId && x.ID == model.SelectedEvent)
                .Include(x => x.Options)
                .Include(x => x.Questions)
                .Include(x => x.Tokens)
                .FirstOrDefault();
            
            // new event, from template
            if (ModelState.IsValid && targetEvent != null && userId.HasValue)
            {
                var newEvent = new Event
                {
                    Name = $"{_localizer["Copy"]} - {targetEvent.Name}",
                    Description = targetEvent.Description,
                    IsPublic = targetEvent.IsPublic,
                    ShowOverallWinner = targetEvent.ShowOverallWinner,
                    UserID = userId.Value,
                    Questions = new List<Question>(),
                    Options = new List<Option>(),
                    Tokens = new List<Token>()
                };

                foreach (var question in targetEvent.Questions)
                {
                    var newQuestion = new Question
                    {
                        Text = question.Text,
                    };
                    newEvent.Questions.Add(newQuestion);
                }

                var optionLookup = new Dictionary<Guid, Option>();
                foreach (var option in targetEvent.Options)
                {
                    var newOption = new Option
                    {
                        Name = option.Name
                    };
                    newEvent.Options.Add(newOption);
                    optionLookup.Add(option.ID, newOption);
                }

                _context.Events.Add(newEvent);

                var tokens = new List<Token>();
                foreach (var token in targetEvent.Tokens)
                {
                    var newToken = new Token
                    {
                        EventID = newEvent.ID,
                        Name = token.Name,
                        Weight = token.Weight
                    };

                    if (token.OptionID.HasValue && optionLookup.ContainsKey(token.OptionID.Value))
                    {
                        newToken.OptionID = optionLookup[token.OptionID.Value].ID;
                    }

                    tokens.Add(newToken);
                }

                _context.Tokens.AddRange(tokens);
                _context.SaveChanges();

                return RedirectToAction("Edit", "Event", new
                {
                    eventId = newEvent.ID
                });
            }

            ModelState.AddModelError("SelectedEvent", "TODO");


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
                .Include(x => x.Votes).ThenInclude(x => x.Choices)
	            .Include(x => x.Votes).ThenInclude(x => x.Token)
                .AsNoTracking()
                .ToList();

            var source = new List<DashboardEventModel>();
            foreach (var record in allEvents)
            {
                var model = new DashboardEventModel
                {
                    Id = record.ID,
                    Name = record.Name,
                    StartDate = record.StartDate,
                    EndDate = record.EndDate,
                    CanActivate = CanActivate(record),
                    CanEdit = CanEdit(record),
                    CanFinish = CanFinish(record)
                };

                // calculate score if event is active
                if (CanFinish(record) || record.EndDate.HasValue)
                {
                    var chartValues = new List<ChartValue>();
                    foreach (var option in record.Options)
                    {
                        var chartValue = new ChartValue
                        {
                            Name = option.Name,
                            Value = 0d
                        };

                        foreach (var recordVote in record.Votes)
                        {
                            var choices = recordVote.Choices.Count(x => x.OptionID == option.ID);

                            if (recordVote.CookieID.HasValue)
                            {
                                chartValue.Value += choices;
                            } else if (recordVote.TokenID.HasValue)
                            {
                                chartValue.Value += choices * (recordVote.Token?.Weight ?? 0);
                            }
                        }

                        chartValues.Add(chartValue);
                    }

                    model.Votes = chartValues;
                }
                else
                {
                    model.Tokens = record.Tokens
                        .GroupBy(x => x.Name, x => x)
                        .Select(x => new ChartValue
                        {
                            Name = x.Key,
                            Value = x.Count()
                        }).ToList();

                    if (model.Tokens.Count == 0)
                    {
                        model.Tokens.Add(new ChartValue
                        {
                            Name = _localizer["No invites"],
                            Value = 0
                        });
                    }
                }

                source.Add(model);
            }
            
            var events = source.Where(x => !x.StartDate.HasValue).ToList();
            var active = source.Where(x => x.StartDate.HasValue && !x.EndDate.HasValue).ToList();
            var finished = source.Where(x => x.StartDate.HasValue && x.EndDate.HasValue).ToList();



            return new EventListModel
            {
                Events = events,
                Active = active,
                Finished = finished,
                CloneEventModel = new CloneEventModel
                {
                    All = events.Concat(active).Concat(finished).ToList()
                }
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

            // event finished (NOTE: issue#8 on github)
            // finished events may be re-activated
            //if (e.EndDate.HasValue) return false;

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
                .Include(x => x.Votes).ThenInclude(x => x.Choices)
                .FirstOrDefault();
        }

        #endregion
    }

    public class EventListModel
    {
        public List<DashboardEventModel> Events { get; set; } = new List<DashboardEventModel>();
        public List<DashboardEventModel> Active { get; set; } = new List<DashboardEventModel>();
        public List<DashboardEventModel> Finished { get; set; } = new List<DashboardEventModel>();
        
        public CloneEventModel CloneEventModel { get; set; } = new CloneEventModel();
    }

    public class CloneEventModel
    {
        public List<DashboardEventModel> All { get; set; } = new List<DashboardEventModel>();

        [Display(Name = "Template")]
        public Guid? SelectedEvent { get; set; }
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

        public List<ChartValue> Votes { get; set; } = new List<ChartValue>();
        public List<ChartValue> Tokens { get; set; } = new List<ChartValue>();
    }
}