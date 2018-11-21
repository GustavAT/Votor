using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
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

        public async Task<IActionResult> Edit(Guid eventId)
        {
            var editEvent = await InitEditEventModel(eventId);

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
            var target = await GetEventById(eventModel.EventId);

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

                if (target.Name != eventModel.EventName)
                {
                    target.Name = eventModel.EventName;
                }

                _context.SaveChanges();

                // todo show status message
            }

            ViewBag.Section = "general";
            return View("Edit", await InitEditEventModel(eventModel.EventId));
        }

        [HttpPost]
        public async Task<IActionResult> AddQuestion(QuestionModel questionModel)
        {
            if (ModelState.IsValid)
            {
                var question = new Question
                {
                    Text = questionModel.Question,
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
                    Name = optionModel.Option,
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

            var tokens = _context.Tokens.Where(x => x.OptionID == optionId);
            _context.RemoveRange(tokens);
            
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
                EventId = target.ID,
                EventName = target.Name
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
            var tokenName = tokenModel.Token;

            var existing = await _context.Tokens
                .AnyAsync(x => x.EventID == tokenModel.EventId && x.Name == tokenName);

            if (ModelState.IsValid && !existing)
            {
                for (var i = tokenModel.Count; i-- > 0;)
                {
                    var token = new Token
                    {
                        Name = tokenName,
                        Weight = tokenModel.Weight,
                        EventID = tokenModel.EventId,
                        OptionID = tokenModel.RestrictionId
                    };

                    // todo: token restriction

                    _context.Tokens.Add(token);
                    _context.SaveChanges();
                }
            }

            ViewBag.Section = "tokens";
            return View("Edit", await InitEditEventModel(tokenModel.EventId));
        }

        
        public async Task<IActionResult> RemoveToken(string token, Guid eventId)
        {
            var tokens = _context.Tokens.Where(x => x.EventID == eventId && x.Name == token);

            _context.Tokens.RemoveRange(tokens);
            _context.SaveChanges();

            ViewBag.Section = "tokens";
            return View("Edit", await InitEditEventModel(eventId));
        }

        public async Task<IActionResult> Details(Guid eventId)
        {
            var targetEvent = await GetEventById(eventId);

            if (targetEvent == null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var tokens = _context.Tokens
                .Where(x => x.EventID == targetEvent.ID)
                .Include(x => x.Option)
                .AsNoTracking()
                .GroupBy(x => x.Name);

            var model = new EventDetailModel
            {
                Id = targetEvent.ID,
                Name = targetEvent.Name,
                Description = targetEvent.Description,
                PublicUrl = GeneratePublicUrl(targetEvent.ID),
                Tokens = tokens.AsEnumerable().Select(TokenDetailModel.Create).ToList()
            };


            return View("Details", model);
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
                Question = x.Text,
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
                Option = x.Name,
                Id = x.ID,
                EventId = x.EventID
            }).ToList();
        }

        public List<TokenModel> GetTokensByEventId(Guid id, List<OptionModel> options)
        {
            var groupedTokens = _context.Tokens.Where(x => x.EventID == id)
                .Include(x => x.Option).AsNoTracking().GroupBy(x => x.Name);

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
                    Token = token.Key,
                    Count = count,
                    RestrictionId = restrictionId,
                    Restriction = restriction,
                    Weight = weight,
                    Options = options
                });

            }

            return tokens;
        }

        private async Task<EditEventModel> InitEditEventModel(Guid eventId)
        {
            var source = await GetEventById(eventId);

            if (source == null) return null;

            var options = GetOptionsByEventId(eventId);
            var model = new EditEventModel
            {
                EventModel = new EventModel
                {
                    EventId = source.ID,
                    EventName = source.Name,
                    Description = source.Description,
                    IsPublic = source.IsPublic,
                    PublicUrl = GeneratePublicUrl(eventId)
                },
                Questions = GetQuestionsByEventId(eventId),
                Options = options,
                Tokens = GetTokensByEventId(eventId, options)
            };

            return model;
        }

        private string GeneratePublicUrl(Guid eventId)
        {
            var request = HttpContext.Request;
            var uriBuilder = new UriBuilder();
            uriBuilder.Scheme = request.Scheme;
            uriBuilder.Host = request.Host.Host;
            //uriBuilder.Path = UrlHelperExtensions.Action(Url, "Index", "VoteController", new
            //{
            //    Area = "Vote",
            //    Id = eventId
            //});
            uriBuilder.Path = UrlHelperExtensions.Action(Url, "Details", "Event", eventId);

            return uriBuilder.ToString();
        }

        #endregion
    }

    public class EventDetailModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        [Display(Name = "Public")]
        public string PublicUrl { get; set; }

        public List<TokenDetailModel> Tokens { get; set; }
    }

    public class TokenDetailModel
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public double Weight { get; set; }
        public string Restriction { get; set; }

        public static TokenDetailModel Create(IGrouping<string, Token> tokens)
        {
            return new TokenDetailModel
            {
                Name = tokens.Key,
                Count = tokens.Count(),
                Weight = tokens.FirstOrDefault()?.Weight ?? 1d,
                Restriction = tokens.FirstOrDefault()?.Option.Name
            };
        }
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
        public Guid EventId { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(100, ErrorMessage = "The {0} field must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Event")]
        public string EventName { get; set; }

        [StringLength(200, ErrorMessage = "The {0} field must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Public")]
        public bool IsPublic { get; set; }

        [DataType(DataType.Url)]
        public string PublicUrl { get; set; }
    }

    public class QuestionModel
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(200, ErrorMessage = "The {0} field must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Question")]
        public string Question { get; set; }
    }

    public class OptionModel
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(100, ErrorMessage = "The {0} field must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Option")]
        public string Option { get; set; }
    }

    public class TokenModel
    {
        public Guid EventId { get; set; }
        
        public Guid? RestrictionId { get; set; }

        [Display(Name = "Restriction")]
        public string Restriction { get; set; }

        [Range(1, 50, ErrorMessage = "The {0} field must be between {1} and  {2}.")]
        [Display(Name = "Count")]
        public int Count { get; set; }
        
        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(100, ErrorMessage = "The {0} field must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Token")]
        public string Token { get; set; }

        [Range(0.1, 100, ErrorMessage = "The {0} field must be between {1} and  {2}.")]
        [Display(Name = "Weight")]
        public double Weight { get; set; }

        public List<OptionModel> Options { get; set; }
    }
}
