using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Votor.Areas.Portal.Data;
using Votor.Areas.Portal.Models;
using Votor.Areas.Voting.Controllers;

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

        public async Task<IActionResult> Edit(Guid eventId, string section = "general")
        {
            var editEvent = await InitEditEventModel(eventId);

            // Event not found or invalid user
            if (editEvent == null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Section = section;
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

                if (target.ShowOverallWinner != eventModel.ShowOverallWinner)
                {
                    target.ShowOverallWinner = eventModel.ShowOverallWinner;
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

                return RedirectToAction("Edit", "Event", new
                {
                    eventId = eventModel.EventId,
                    section = "general"
                });
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

                return RedirectToAction("Edit", "Event", new
                {
                    eventId = questionModel.EventId,
                    section = "questions"
                });
            }

            ViewBag.Section = "questions";
            return View("Edit", await InitEditEventModel(questionModel.EventId));
        }

        public IActionResult RemoveQuestion(Guid questionId, Guid eventId)
        {
            var question = new Question
            {
                ID = questionId
            };
            _context.Questions.Attach(question);
            _context.Questions.Remove(question);
            _context.SaveChanges();

            return RedirectToAction("Edit", "Event", new
            {
                eventId,
                section = "questions"
            });
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

                return RedirectToAction("Edit", "Event", new
                {
                    eventId = optionModel.EventId,
                    section = "options"
                });
            }

            ViewBag.Section = "options";
            return View("Edit", await InitEditEventModel(optionModel.EventId));
        }

        public IActionResult RemoveOption(Guid optionId, Guid eventId)
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

            return RedirectToAction("Edit", "Event", new
            {
                eventId,
                section = "options"
            });
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
                var votes = _context.Votes.Where(x => x.EventID == eventId)
                    .Include(x => x.Choices);

                var choices = votes.SelectMany(x => x.Choices);

                _context.RemoveRange(choices);
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
                    
                    _context.Tokens.Add(token);
                    _context.SaveChanges();
                }

                return RedirectToAction("Edit", "Event", new
                {
                    eventId = tokenModel.EventId,
                    section = "tokens"
                });
            }

            ViewBag.Section = "tokens";
            return View("Edit", await InitEditEventModel(tokenModel.EventId));
        }


        public IActionResult RemoveToken(string token, Guid eventId)
        {
            var tokens = _context.Tokens.Where(x => x.EventID == eventId && x.Name == token);

            _context.Tokens.RemoveRange(tokens);
            _context.SaveChanges();

            return RedirectToAction("Edit", "Event", new
            {
                eventId,
                section = "tokens"
            });
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

            var tokenModels = new List<TokenDetailModel>();

            foreach (var grouping in tokens)
            {
                var view = new TokenDetailModel
                {
                    Name = grouping.Key,
                    Count = grouping.Count(),
                    Weight = grouping.FirstOrDefault()?.Weight ?? 1d,
                    Restriction = grouping.FirstOrDefault()?.Option?.Name,
                    TokenUrls = grouping.Select(x => GenerateVotingUrl(x.ID, HttpContext)).ToList()
                };

                tokenModels.Add(view);
            }

            var model = new EventDetailModel
            {
                Id = targetEvent.ID,
                Name = targetEvent.Name,
                Description = targetEvent.Description,
                PublicUrl = targetEvent.IsPublic ? GenerateVotingUrl(targetEvent.ID, HttpContext) : string.Empty,
                ShowOverallWinner = targetEvent.ShowOverallWinner,
                Tokens = tokenModels,
                StartDate = targetEvent.StartDate,
                EndDate = targetEvent.EndDate
            };

            // calculate score if event is active
            if (targetEvent.StartDate.HasValue || targetEvent.EndDate.HasValue)
            {
                var chartValues = new List<ChartValue>();
                foreach (var option in targetEvent.Options)
                {
                    var chartValue = new ChartValue
                    {
                        Name = option.Name,
                        Value = 0d
                    };

                    foreach (var recordVote in targetEvent.Votes)
                    {
                        var choices = recordVote.Choices.Count(x => x.OptionID == option.ID);

                        if (recordVote.CookieID.HasValue)
                        {
                            chartValue.Value += choices;
                        }
                        else if (recordVote.TokenID.HasValue)
                        {
                            chartValue.Value += choices * (recordVote.Token?.Weight ?? 0);
                        }
                    }

                    chartValues.Add(chartValue);
                }

                model.ChartValues = chartValues;
            }
            else
            {
                model.TokenValues = targetEvent.Tokens
                    .GroupBy(x => x.Name, x => x)
                    .Select(x => new ChartValue
                    {
                        Name = x.Key,
                        Value = x.Count()
                    }).ToList();

                if (model.TokenValues.Count == 0)
                {
                    model.TokenValues.Add(new ChartValue
                    {
                        Name = _localizer["No invites"],
                        Value = 0
                    });
                }
            }


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
            return _context.Events
                .Include(x => x.Options)
                .Include(x => x.Votes).ThenInclude(x => x.Choices)
                .Include(x => x.Votes).ThenInclude(x => x.Token)
                .FirstOrDefault(x => x.ID == id && x.UserID == userId);
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
                    ShowOverallWinner = source.ShowOverallWinner
                },
                Questions = GetQuestionsByEventId(eventId),
                Options = options,
                Tokens = GetTokensByEventId(eventId, options)
            };

            return model;
        }

        public static string GenerateVotingUrl(Guid id, HttpContext context)
        {
            var request = context.Request;
            var uriBuilder = new UriBuilder {Scheme = request.Scheme, Host = request.Host.Host};
            if (request.Host.Port.HasValue)
            {
                uriBuilder.Port = request.Host.Port.Value;
            }
            uriBuilder.Path = $"/Vote/{id}";
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
        public bool ShowOverallWinner { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public List<TokenDetailModel> Tokens { get; set; }

        public List<ChartValue> TokenValues { get; set; } = new List<ChartValue>();
        public List<ChartValue> ChartValues { get; set; } = new List<ChartValue>();
    }

    public class TokenDetailModel
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public double Weight { get; set; }
        public string Restriction { get; set; }
        public List<string> TokenUrls { get; set; }
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
        [DataType(DataType.Text)]
        [Display(Name = "Name")]
        public string EventName { get; set; }

        [StringLength(200, ErrorMessage = "The {0} field must be at least {2} and at max {1} characters long.", MinimumLength = 0)]
        [DataType(DataType.Text)]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Public")]
        public bool IsPublic { get; set; }

        [Display(Name = "Show overall winner in results")]
        public bool ShowOverallWinner { get; set; }
    }

    public class QuestionModel
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [DataType(DataType.Text)]
        [Display(Name = "Question")]
        public string Question { get; set; }
    }

    public class OptionModel
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [DataType(DataType.Text)]
        [Display(Name = "Choice")]
        public string Option { get; set; }
    }

    public class TokenModel
    {
        public Guid EventId { get; set; }
        
        public Guid? RestrictionId { get; set; }

        [Display(Name = "Restriction")]
        public string Restriction { get; set; }

        [Range(1, 100, ErrorMessage = "The {0} field must be between {1} and  {2}.")]
        [Display(Name = "Count")]
        public int Count { get; set; }
        
        [Required(ErrorMessage = "The {0} field is required.")]
        [DataType(DataType.Text)]
        [Display(Name = "Invite")]
        public string Token { get; set; }

        [Range(0.1, 100, ErrorMessage = "The {0} field must be between {1} and  {2}.")]
        [Display(Name = "Weight")]
        public double Weight { get; set; }

        public List<OptionModel> Options { get; set; }
    }
}
