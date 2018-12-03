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
            else
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

            if (voteModel.Completed)
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
                EventName = targetEvent.Name
            };
            
            var votes = _context.Votes
                .Include(x => x.Choices).ThenInclude(x => x.Option)
                .Include(x => x.Choices).ThenInclude(x => x.Question)
                .Include(x => x.Token)
                .Where(x => x.EventID == eventId)
                .AsNoTracking()
                .ToList();

            var tokenVotes = votes.Where(x => x.TokenID.HasValue).ToList();
            var publicVotes = votes.Where(x => x.CookieID.HasValue).ToList();

            // distribution public vs tokens (all)
            var chart = new ChartModel
            {
                Name = _localizer["Votes"],
                Values = new List<ChartValue>
                {
                    new ChartValue
                    {
                        Name = _localizer["Public"],
                        Value = publicVotes.Count()
                    },
                    new ChartValue
                    {
                        Name = _localizer["Invite"],
                        Value = tokenVotes.Count()
                    }
                }
            };
            model.VoteDistribution = chart;

            var weightedTokenCount = votes.Where(x => x.TokenID.HasValue)
                .Select(x => x.Token.Weight)
                .Sum();

            // weighted distribution public vs tokens (all)
            var weightedChart = new ChartModel
            {
                Name = _localizer["Weighted Votes"],
                Values = new List<ChartValue>
                {
                    new ChartValue
                    {
                        Name = _localizer["Public"],
                        Value = publicVotes.Count
                    },
                    new ChartValue
                    {
                        Name = _localizer["Invite"],
                        Value = weightedTokenCount
                    }
                }
            };
            
            model.WeightedVoteDistribution = weightedChart;

            model.Questions = new List<QuestionChartModel>();

            var choices = votes.SelectMany(x => x.Choices).ToList();

            foreach (var question in targetEvent.Questions)
            {
                var questionModel = new QuestionChartModel
                {
                    Question = question.Text,
                    Values = new List<ChartValue>(),
                    WeightedValues = new List<ChartValue>()
                };

                foreach (var option in targetEvent.Options)
                {
                    questionModel.Values.Add(new ChartValue
                    {
                        Name = option.Name,
                        Value = choices.Count(x => x.OptionID == option.ID && x.QuestionID == question.ID)
                    });


                    double count = votes.Where(x => x.CookieID.HasValue)
                        .SelectMany(x => x.Choices).Count(x => x.OptionID == option.ID && x.QuestionID == question.ID);

                    var votesWithToken = votes.Where(x => x.Token != null);
                    foreach (var vote in votesWithToken)
                    {
                        var weight = vote.Token.Weight;
                        count += weight * vote.Choices.Count(x => x.OptionID == option.ID && x.QuestionID == question.ID);
                    }

                    questionModel.WeightedValues.Add(new ChartValue
                    {
                        Name = option.Name,
                        Value = count
                    });
                }

                model.Questions.Add(questionModel);
            }

            var overall = new QuestionChartModel
            {
                Question = _localizer["Overall"],
                Values = new List<ChartValue>(),
                WeightedValues = new List<ChartValue>()
            };

            var publicChoices = publicVotes.SelectMany(x => x.Choices).ToList();
            var tokenChoices = tokenVotes.SelectMany(x => x.Choices).ToList();

            foreach (var option in targetEvent.Options)
            {
                overall.Values.Add(new ChartValue
                {
                    Name = option.Name,
                    Value = choices.Count(x => x.OptionID == option.ID)
                });

                var choicesForOption = tokenChoices.Where(x => x.OptionID == option.ID)
                    .Join(votes, x => x.VoteID, x => x.ID, (choice, vote) => new { Choice = choice, Vote = vote});



                double count = publicChoices.Count(x => x.OptionID == option.ID);
                foreach (var choice in choicesForOption)
                {
                    count += choice.Vote.Token?.Weight ?? 1;
                }

                overall.WeightedValues.Add(new ChartValue
                {
                    Name = option.Name,
                    Value = count
                });
            }

            model.Overall = overall;


            return View("Result", model);
        }

        private Event GetActiveEventById(Guid eventId)
        {
            return _context.Events
                .Where(x => x.ID == eventId && x.IsPublic
                                            && x.StartDate.HasValue
                                            && !x.EndDate.HasValue)
                .Include(x => x.Questions)
                .AsNoTracking().FirstOrDefault();
        }

        private Event GetFinishedEventById(Guid eventId)
        {
            return _context.Events
                .Where(x => x.ID == eventId && x.IsPublic
                                            && x.StartDate.HasValue
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
        
        public ChartModel VoteDistribution { get; set; }
        public ChartModel WeightedVoteDistribution { get; set; }

        public QuestionChartModel Overall { get; set; }

        public List<QuestionChartModel> Questions { get; set; } = new List<QuestionChartModel>();
    }

    public class ChartModel
    {
        public string Name { get; set; }
        public List<ChartValue> Values { get; set; } = new List<ChartValue>();
    }

    public class QuestionChartModel
    {
        public string Question { get; set; }
        public List<ChartValue> Values { get; set; } = new List<ChartValue>();
        public List<ChartValue> WeightedValues { get; set; } = new List<ChartValue>();
    }

    public class ChartValue {
        public string Name { get; set; }
        public double Value { get; set; }
    }
}