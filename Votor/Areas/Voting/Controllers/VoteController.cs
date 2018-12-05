using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Votor.Areas.Portal.Controllers;
using Votor.Areas.Portal.Data;
using Votor.Areas.Portal.Models;

namespace Votor.Areas.Voting.Controllers
{
    [Area("Voting")]
    public class VoteController : Controller
    {
        private readonly VotorContext _context;
        public readonly IStringLocalizer<SharedResources> _localizer;

        public VoteController(VotorContext context, IStringLocalizer<SharedResources> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        /// <summary>
        /// Vote for an event
        /// Id = token id of an active/inactive event or event-id (active/inactive)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Index(Guid id)
        {
            var targetEvent = GetActiveEventById(id);
            Token token;

            Vote targetVote = null;

            if (targetEvent == null)
            {
                token = GetTokenById(id);

                if (token == null)
                {
                    var finishedEvent = GetFinishedEventById(id);

                    if (finishedEvent == null)
                    {
                        return View("NotFound");
                    }

                    return RedirectToAction("Result", "Vote", new
                    {
                        eventId = finishedEvent.ID
                    });
                }

                targetEvent = GetActiveEventById(token.EventID);

                if (targetEvent == null)
                {
                    var finishedEvent = GetFinishedEventById(token.EventID);

                    if (finishedEvent == null)
                    {
                        return View("NotFound");
                    }
                    
                    return RedirectToAction("Result", "Vote", new
                    {
                        eventId = finishedEvent.ID
                    });
                }

                // token vote

                targetVote = _context.Votes
                    .Where(x => x.TokenID == token.ID)
                    .AsNoTracking()
                    .FirstOrDefault();

                if (targetVote == null)
                {
                    targetVote = InitializeNewVote(targetEvent, tokenId: token.ID);
                }
            }
            else if (targetEvent.IsPublic)
            {
                // public vote

                // check if a cookie is set
                var publicTokenId = GetPublicToken(id);

                if (publicTokenId.HasValue)
                {
                    // load model from db (via the public token)
                    var tokenId = publicTokenId;

                    targetVote = _context.Votes
                        .Where(x => x.CookieID == tokenId.Value && x.EventID == id)
                        .AsNoTracking().FirstOrDefault();
                }

                if (targetVote == null)
                {
                    // No token, token expired, no vote for token

                    // init cookie
                    publicTokenId = Guid.NewGuid();
                    SetPublicToken(publicTokenId.Value, id);

                    // create new vote with public id
                    targetVote = InitializeNewVote(targetEvent, publicTokenId);
                }
            }


            // create the model
            if (targetVote != null)
            {
                var voteModel = InitVoteModel(targetVote.ID);

                if (voteModel != null)
                {
                    return View("Index", voteModel);
                }
            }

            return View("NotFound");
        }

        public IActionResult SaveVoting(VoteModel voteModel)
        {
            var targetEvent = GetActiveEventById(voteModel.EventId);

            if (targetEvent == null) return View("NotFound");

            var choices = _context.Choices
                .Where(x => x.VoteID == voteModel.VoteId)
                .ToList();

            foreach (var choiceModel in voteModel.Choices)
            {
                var choice = choices.FirstOrDefault(x => x.ID == choiceModel.Id);

                if (choice == null) continue;

                choice.OptionID = choiceModel.OptionId;
            }

            _context.SaveChanges();

            if (voteModel.Completed && voteModel.Choices.All(x => x.OptionId.HasValue))
            {
                var vote = _context.Votes
                    .FirstOrDefault(x => x.ID == voteModel.VoteId);

                if (vote != null)
                {
                    vote.IsCompleted = true;
                    _context.SaveChanges();
                }
            }
            
            return RedirectToAction("Index", new
            {
                id = voteModel.PublicToken.HasValue ? voteModel.EventId : voteModel.Token
            });
        }

        [HttpGet]
        public IActionResult Result(Guid eventId)
        {
            var targetEvent = GetFinishedEventById(eventId);

            if (targetEvent == null)
            {
                return View("NotFound");
            }

            var model = new ResultModel
            {
                EventId = targetEvent.ID,
                EventName = targetEvent.Name,
                EventDescription = targetEvent.Description
            };
            
            var votes = _context.Votes
                .Include(x => x.Choices).ThenInclude(x => x.Option)
                .Include(x => x.Choices).ThenInclude(x => x.Question)
                .Include(x => x.Token)
                .Where(x => x.EventID == eventId && x.IsCompleted)
                .AsNoTracking()
                .ToList();


            var allChoices = votes.SelectMany(x => x.Choices).ToList();
            var tokenChoices = allChoices.Where(x => x.Vote.TokenID.HasValue).ToList();
            var publicChoices = allChoices.Where(x => x.Vote.CookieID.HasValue).ToList();

            model.PublicVotes = votes.Count(x => x.CookieID.HasValue);
            model.TokenVotes = votes.Count(x => x.TokenID.HasValue);
            
            model.Score = new DistributionPieChartModel
            {
                Title = _localizer["Score"],
                PublicVotes = publicChoices.Count,
                TokenVotes = tokenChoices.Sum(x => x.Vote.Token?.Weight ?? 0)
            };

            model.Distribution = new DistributionPieChartModel
            {
                Title = _localizer["Votes"],
                PublicVotes = model.PublicVotes,
                TokenVotes = model.TokenVotes
            };

            model.Questions = new List<QuestionBarChartModel>();

            foreach (var question in targetEvent.Questions)
            {
                var questionModel = new QuestionBarChartModel
                {
                    Title = question.Text,
                    PublicValues = new List<ChartValue>(),
                    TokenValues = new List<ChartValue>(),
                    Score = new List<ChartValue>()
                };

                foreach (var option in targetEvent.Options)
                {
                    var tokenValue = new ChartValue
                    {
                        Name = option.Name,
                        Value = tokenChoices.Where(x => x.QuestionID == question.ID && x.OptionID == option.ID)
                            .Sum(x => x.Vote.Token?.Weight ?? 0)
                    };
                    questionModel.TokenValues.Add(tokenValue);

                    var publicValue = new ChartValue
                    {
                        Name = option.Name,
                        Value = publicChoices.Count(x => x.QuestionID == question.ID && x.OptionID == option.ID)
                    };
                    questionModel.PublicValues.Add(publicValue);

                    questionModel.Score.Add(new ChartValue
                    {
                        Name = option.Name,
                        Value = tokenValue.Value + publicValue.Value
                    });
                }

                model.Questions.Add(questionModel);
            }
            
            model.Overall = new QuestionBarChartModel
            {
                Title = _localizer["Overall"],
                PublicValues = new List<ChartValue>(),
                TokenValues = new List<ChartValue>(),
                Score = new List<ChartValue>()
            };

            foreach (var option in targetEvent.Options)
            {
                var tokenValue = new ChartValue
                {
                    Name = option.Name,
                    Value = tokenChoices.Where(x => x.OptionID == option.ID).Sum(x => x.Vote.Token?.Weight ?? 0)
                };
                model.Overall.TokenValues.Add(tokenValue);

                var publicValue = new ChartValue
                {
                    Name = option.Name,
                    Value = publicChoices.Count(x => x.OptionID == option.ID)
                };
                model.Overall.PublicValues.Add(publicValue);

                model.Overall.Score.Add(new ChartValue
                {
                    Name = option.Name,
                    Value = tokenValue.Value + publicValue.Value
                });
            }

            return View("Result", model);
        }

        private Event GetActiveEventById(Guid eventId)
        {
            return _context.Events
                .Where(x => x.ID == eventId && x.StartDate.HasValue
                                            && !x.EndDate.HasValue)
                .Include(x => x.Questions)
                .AsNoTracking().FirstOrDefault();
        }

        private Event GetFinishedEventById(Guid eventId)
        {
            return _context.Events
                .Where(x => x.ID == eventId && x.StartDate.HasValue
                                            && x.EndDate.HasValue)
                .Include(x => x.Questions)
                .Include(x => x.Options)
                .Include(x => x.Tokens)
                .AsNoTracking().FirstOrDefault();
        }

        private Token GetTokenById(Guid tokenId)
        {
            return _context.Tokens.Where(x => x.ID == tokenId).AsNoTracking().FirstOrDefault();
        }

        private Guid? GetPublicToken(Guid eventId)
        {
            var cookie = Request.Cookies[$"{eventId}"];
            return Util.ParseGuid(cookie);
        }

        private void SetPublicToken(Guid tokenId, Guid eventId)
        {
            Response.Cookies.Append($"{eventId}", $"{tokenId}", new CookieOptions
            {
                Expires = DateTime.UtcNow.AddMinutes(3600),
                HttpOnly = true,
                Secure = true
            });
        }

        private Vote InitializeNewVote(Event targetEvent, Guid? publicCookieId = null, Guid? tokenId = null)
        {
            if (!publicCookieId.HasValue && !tokenId.HasValue) return null;

            var vote = new Vote
            {
                EventID = targetEvent.ID,
                IsCompleted = false,
                Choices = new List<Choice>()
            };

            if (publicCookieId.HasValue)
            {
                // create new public vote
                vote.CookieID = publicCookieId;

            } else
            {
                // create new token vote
                vote.TokenID = tokenId;
            }

            _context.Votes.Attach(vote);
            //_context.SaveChanges();



            foreach (var question in targetEvent.Questions)
            {
                var choice = new Choice
                {
                    Vote = vote,
                    QuestionID = question.ID
                };
                _context.Choices.Attach(choice);
            }
            
            _context.SaveChanges();

            return vote;
        }

        /// <summary>
        /// Initialize the <see cref="VoteModel"/>
        /// </summary>
        /// <param name="voteId">Id of vote</param>
        /// <returns></returns>
        private VoteModel InitVoteModel(Guid voteId)
        {
            var vote = _context.Votes
                .Where(x => x.ID == voteId)
                .Include(x => x.Choices).ThenInclude(x => x.Option)
                .Include(x => x.Choices).ThenInclude(x => x.Question)
                .Include(x => x.Token)
                .Include(x => x.Event).ThenInclude(x => x.Options)
                .AsNoTracking()
                .FirstOrDefault();

            if (vote?.Event == null) return null;

            return new VoteModel
            {
                VoteId = vote.ID,
                Token = vote.TokenID,
                PublicToken = vote.CookieID,
                Completed = vote.IsCompleted,

                Choices = InitChoiceModel(vote),

                //Options = InitOptionModel(vote),

                EventId = vote.EventID,
                EventName = vote.Event.Name,
                EventDescription = vote.Event.Description
            };
        }

        /// <summary>
        /// Initialize the selected choices
        /// </summary>
        /// <param name="vote"></param>
        /// <returns></returns>
        private List<ChoiceModel> InitChoiceModel(Vote vote)
        {
            var choices = vote.Choices;

            return choices.Select(x => new ChoiceModel
            {
                Id = x.ID,
                Option = x.Option?.Name ?? string.Empty,
                OptionId = x.OptionID,
                Question = x.Question.Text,
                QuestionId = x.QuestionID,
                Options = InitOptionModel(vote)
            }).ToList();
        }

        /// <summary>
        /// Initialize the available options (restrictions applied)
        /// </summary>
        /// <param name="vote"></param>
        /// <returns></returns>
        private List<OptionModel> InitOptionModel(Vote vote)
        {
            var token = vote.Token;

            // select all options that are not restricted by a token
            return vote.Event.Options.Where(x => x.ID != token?.OptionID)
                .Select(x => new OptionModel
                {
                    EventId = x.EventID,
                    Id = x.ID,
                    Option = x.Name
                }).ToList();
        }
    }

    public class VoteModel
    {
        public Guid VoteId { get; set; }
        public bool Completed { get; set; }
        
        public Guid? Token { get; set; }

        public Guid? PublicToken { get; set; }

        public Guid EventId { get; set; }
        public string EventName { get; set; }
        public string EventDescription { get; set; }

        // actual selected options for questions
        public List<ChoiceModel> Choices { get; set; }
    }

    public class ChoiceModel
    {
        public Guid Id { get; set; }
        public Guid QuestionId { get; set; }
        public string Question { get; set; }
        public Guid? OptionId { get; set; }
        public string Option { get; set; }

        public List<OptionModel> Options { get; set; }
    }

    public class ResultModel
    {
        public Guid EventId { get; set; }
        public string EventName { get; set; }
        public string EventDescription { get; set; }

        public int PublicVotes { get; set; }
        public int TokenVotes { get; set; }

        public DistributionPieChartModel Score { get; set; }
        public DistributionPieChartModel Distribution { get; set; }

        public QuestionBarChartModel Overall { get; set; }

        public List<QuestionBarChartModel> Questions { get; set; } = new List<QuestionBarChartModel>();
    }

    public class DistributionPieChartModel
    {
        public string Title { get; set; }
        public double PublicVotes { get; set; }
        public double TokenVotes { get; set; }
    }

    
    public class QuestionBarChartModel
    {
        public string Title { get; set; }
        public List<ChartValue> PublicValues { get; set; }
        public List<ChartValue> TokenValues { get; set; }
        public List<ChartValue> Score { get; set; }
    }

    public class ChartValue {
        public string Name { get; set; }
        public double Value { get; set; }
    }
}