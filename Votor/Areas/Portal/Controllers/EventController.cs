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

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Votor.Areas.Portal.Controllers
{
    [Area("Portal")]
    [Authorize]
    public class EventController : Controller
    {
        private VotorContext _context;
        private UserManager<IdentityUser> _userManager;
        private IStringLocalizer<SharedResources> _localizer;
        
        public EventController(VotorContext context, UserManager<IdentityUser> userManager,
            IStringLocalizer<SharedResources> localizer)
        {
            _context = context;
            _userManager = userManager;
            _localizer = localizer;
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var editEvent = await InitEditEventModel(id);

            // Event not found or invalid user
            if (editEvent == null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Section = "general";
            return View("Edit", editEvent);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EventModel eventModel)
        {
            var target = await GetEventById(eventModel.Id);

            // Event not found or invalid user
            if (target == null) return RedirectToAction("Index", "Dashboard");

            if (ModelState.IsValid)
            {
                if (target.IsPublic != eventModel.IsPublic)
                {
                    target.IsPublic = eventModel.IsPublic;
                }

                if (target.Description != eventModel.Description)
                {
                    target.Description = eventModel.Description;
                }

                if (target.Name != eventModel.Name)
                {
                    target.Name = eventModel.Name;
                }

                _context.SaveChanges();

                // todo show status message
            }

            ViewBag.Section = "general";
            return View("Edit", await InitEditEventModel(eventModel.Id));
        }

        [HttpPost]
        public async Task<IActionResult> AddQuestion(QuestionModel questionModel)
        {
            if (ModelState.IsValid)
            {
                var question = new Question
                {
                    Text = questionModel.Text,
                    EventID = questionModel.EventId
                };

                _context.Questions.Add(question);
                _context.SaveChanges();
            }

            ViewBag.Section = "questions";
            return View("Edit", await InitEditEventModel(questionModel.EventId));
        }
        
        public async Task<IActionResult> RemoveQuestion(Guid questionId, Guid eventId)
        {
            var question = new Question
            {
                ID = questionId
            };
            _context.Questions.Attach(question);
            _context.Questions.Remove(question);
            _context.SaveChanges();

            ViewBag.Section = "questions";
            return View("Edit", await InitEditEventModel(eventId));
        }

        [HttpPost]
        public async Task<IActionResult> AddOption(OptionModel optionModel)
        {
            if (ModelState.IsValid)
            {
                var option = new Option
                {
                    Name = optionModel.Name,
                    EventID = optionModel.EventId
                };

                _context.Options.Add(option);
                _context.SaveChanges();
            }

            ViewBag.Section = "options";
            return View("Edit", await InitEditEventModel(optionModel.EventId));
        }

        public async Task<IActionResult> RemoveOption(Guid optionId, Guid eventId)
        {
            var option = new Option
            {
                ID = optionId
            };
            _context.Options.Attach(option);
            _context.Options.Remove(option);
            _context.SaveChanges();

            ViewBag.Section = "options";
            return View("Edit", await InitEditEventModel(eventId));
        }

        public async Task<IActionResult> DeleteWithPromt(Guid eventId)
        {
            var target = await GetEventById(eventId);

            if (target == null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View("Delete", new EventModel
            {
                Id = target.ID,
                Name = target.Name
            });
        }
        
        public async Task<IActionResult> Delete(Guid eventId)
        {
            var target = await GetEventById(eventId);

            if (target != null)
            {
                var votes = _context.Votes.Where(x => x.EventID == eventId);
                _context.RemoveRange(votes);

                var tokens = _context.Tokens.Where(x => x.EventID == eventId);
                _context.RemoveRange(tokens);

                var options = _context.Options.Where(x => x.EventID == eventId);
                _context.RemoveRange(options);

                var questions = _context.Questions.Where(x => x.EventID == eventId);
                _context.RemoveRange(questions);

                _context.Remove(target);

                _context.SaveChanges();

                ViewBag.StatusMessage = _localizer["The event {0} was deleted!", target.Name];
            }

            return RedirectToAction("Index", "Dashboard");
        }

        public async Task<IActionResult> AddToken(TokenModel tokenModel)
        {
            if (ModelState.IsValid)
            {
                for (var i = tokenModel.Count; i-- > 0;)
                {
                    var token = new Token
                    {
                        Name = tokenModel.Name,
                        Weight = tokenModel.Weight,
                        EventID = tokenModel.EventId
                    };

                    // todo: token restriction

                    _context.Tokens.Add(token);
                    _context.SaveChanges();
                }
            }

            ViewBag.Section = "tokens";
            return View("Edit", await InitEditEventModel(tokenModel.EventId));
        }

        #region helper

        private async Task<Guid?> GetUserId()
        {
            var user = await _userManager.GetUserAsync(User);
            return Util.ParseGuid(user?.Id);
        }
        
        private async Task<Event> GetEventById(Guid id)
        {
            var userId = await GetUserId();
            return _context.Events.FirstOrDefault(x => x.ID == id && x.UserID == userId);
        }

        private List<QuestionModel> GetQuestionsByEventId(Guid id)
        {
            var questions = _context.Questions.Where(x => x.EventID == id)
                .AsNoTracking().ToList();

            return questions.Select(x => new QuestionModel
            {
                Text = x.Text,
                Id = x.ID,
                EventId = x.EventID
            }).ToList();
        }

        private List<OptionModel> GetOptionsByEventId(Guid id)
        {
            var options = _context.Options.Where(x => x.EventID == id)
                .AsNoTracking().ToList();

            return options.Select(x => new OptionModel
            {
                Name = x.Name,
                Id = x.ID,
                EventId = x.EventID
            }).ToList();
        }

        public List<TokenModel> GetTokensByEventId(Guid id)
        {
            var groupedTokens = _context.Tokens.Where(x => x.EventID == id)
                .AsNoTracking().GroupBy(x => x.Name);

            var tokens = new List<TokenModel>();
            foreach (var token in groupedTokens)
            {
                var count = token.Count();

                var restrictionId = token.FirstOrDefault()?.OptionID;
                var restriction = token.FirstOrDefault()?.Option?.Name;
                var weight = token.FirstOrDefault()?.Weight ?? 1d;

                tokens.Add(new TokenModel
                {
                    EventId = id,
                    Name = token.Key,
                    Count = count,
                    RestrictionId = restrictionId,
                    Restriction = restriction,
                    Weight = weight
                });

            }

            return tokens;
        }

        private async Task<EditEventModel> InitEditEventModel(Guid eventId)
        {
            var source = await GetEventById(eventId);

            if (source == null) return null;

            var model = new EditEventModel
            {
                EventModel = new EventModel
                {
                    Id = source.ID,
                    Name = source.Name,
                    Description = source.Description,
                    IsPublic = source.IsPublic
                },
                Questions = GetQuestionsByEventId(eventId),
                Options = GetOptionsByEventId(eventId)
            };

            return model;
        }

        #endregion
    }

    public class EditEventModel
    {
        public EventModel EventModel { get; set; }

        public List<QuestionModel> Questions { get; set; }

        public List<OptionModel> Options { get; set; }

        public List<TokenModel> Tokens { get; set; }
    }

    public class EventModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        public string Name { get; set; }

        [StringLength(200, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        public string Description { get; set; }

        public bool IsPublic { get; set; }
    }

    public class QuestionModel
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(200, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        public string Text { get; set; }
    }

    public class OptionModel
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        public string Name { get; set; }
    }

    public class TokenModel
    {
        public Guid EventId { get; set; }
        
        public Guid? RestrictionId { get; set; }
        public string Restriction { get; set; }

        public int Count { get; set; }
        
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        public string Name { get; set; }

        public double Weight { get; set; }
    }
}
