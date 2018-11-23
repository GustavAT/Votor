using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Votor.Areas.Portal.Controllers;
using Votor.Areas.Portal.Data;
using Votor.Areas.Portal.Models;

namespace Votor.Areas.Voting.Controllers
{
    [Area("Voting")]
    public class VoteController : Controller
    {
        private VotorContext _context;

        public VoteController(VotorContext context)
        {
            _context = context;
        }

        [HttpGet]
        public <async Task<IActionResult> Index(Guid id)
        {
            var targetEvent = GetActiveEventById(id);
            Token token;

            Vote targetVote = null;

            if (targetEvent == null)
            {
                token = GetTokenById(id);
                targetEvent = GetActiveEventById(token?.EventID ?? Guid.NewGuid());

                if (token == null || targetEvent == null)
                {
                    // todo return vote not found
                    return View("NotFound");
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
                var publicTokenId = GetPublicToken();

                if (publicTokenId.HasValue)
                {
                    // load model from db (via the public token)
                    var tokenId = publicTokenId;

                    targetVote = _context.Votes
                        .Where(x => x.CookieID == tokenId.Value)
                        .AsNoTracking().FirstOrDefault();
                }
                
                if (targetVote == null)
                {
                    // No token, token expired, no vote for token
                    
                    // init cookie
                    publicTokenId = Guid.NewGuid();
                    SetPublicToken(publicTokenId.Value);

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
        
        public IActionResult SaveVoting(VoteModel voteModel, bool? complete = false)
        {
            var choices = _context.Choices
                .Where(x => x.VoteID == voteModel.Id)
                .ToList();

            foreach (var choiceModel in voteModel.Choices)
            {
                var choice = choices.FirstOrDefault(x => x.ID == choiceModel.Id);

                if (choice == null) continue;

                choice.OptionID = choiceModel.OptionId;
            }

            _context.SaveChanges();

            return View("Index", voteModel);
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

        private Token GetTokenById(Guid tokenId)
        {
            return _context.Tokens.Where(x => x.ID == tokenId).AsNoTracking().FirstOrDefault();
        }

        private Guid? GetPublicToken()
        {
            var cookie = Request.Cookies["Token"];
            return Util.ParseGuid(cookie);
        }

        private void SetPublicToken(Guid tokenId)
        {
            Response.Cookies.Append("Token", $"{tokenId}", new CookieOptions
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
                Id = vote.ID,
                Token = vote.TokenID,
                PublicToken = vote.CookieID,
                Completed = vote.IsCompleted,

                Choices = InitChoiceModel(vote),

                Options = InitOptionModel(vote),

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
                QuestionId = x.QuestionID
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
        public Guid Id { get; set; }
        public bool Completed { get; set; }
        
        public Guid? Token { get; set; }

        public Guid? PublicToken { get; set; }

        public Guid EventId { get; set; }
        public string EventName { get; set; }
        public string EventDescription { get; set; }

        // actual selected options for questions
        public List<ChoiceModel> Choices { get; set; }

        // list of available options (restriction)
        public List<OptionModel> Options { get; set; }
    }

    public class ChoiceModel
    {
        public Guid Id { get; set; }
        public Guid QuestionId { get; set; }
        public string Question { get; set; }
        public Guid? OptionId { get; set; }
        public string Option { get; set; }
    }
}