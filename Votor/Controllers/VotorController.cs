using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Votor.Areas.Portal.Data;

namespace Votor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("WedenigsPolicy")]
    public class VotorController : ControllerBase
    {
        private readonly VotorContext _context;

        public VotorController(VotorContext context)
        {
            _context = context;
        }

        [HttpGet("{eventId}/{userId}")]
        public async Task<ActionResult<ApiEvent>> GetResultAsync(Guid eventId, Guid userId)
        {
            var targetEvent = await _context.Events
                .Include(x => x.Questions)
                .Include(x => x.Options)
                .Include(x => x.Tokens)
                .Include(x => x.Votes).ThenInclude(x => x.Choices)
                .Include(x => x.Votes).ThenInclude(x => x.Token)
                .Include(x => x.BonusPoints)
                .Where(x => x.ID == eventId && x.UserID == userId/* && x.EndDate.HasValue*/)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (targetEvent == null) return NotFound();

            var publicChoices = targetEvent.Votes.Where(x => x.CookieID.HasValue && x.IsCompleted)
                .SelectMany(x => x.Choices).ToList();
            var inviteChoices = targetEvent.Votes.Where(x => x.TokenID.HasValue && x.IsCompleted)
                .SelectMany(x => x.Choices).ToList();
            var bonusPoints = targetEvent.BonusPoints;

            var apiVotes = new List<ApiVote>();
            foreach (var question in targetEvent.Questions)
            {
                var vote = new ApiVote
                {
                    QuestionId = question.ID,
                    Question = question.Text,
                    Choices = new List<ApiChoice>()
                };

                foreach (var option in targetEvent.Options)
                {
                    var publicScore = publicChoices.Count(x => x.OptionID == option.ID && x.QuestionID == question.ID);
                    var inviteScore = inviteChoices.Where(x => x.OptionID == option.ID && x.QuestionID == question.ID)
                        .Sum(x => x.Vote.Token.Weight);
                    var bonusScore = bonusPoints.Where(x => x.OptionID == option.ID && x.QuestionID == question.ID)
                        .Sum(x => x.Points);

                    vote.Choices.Add(new ApiChoice
                    {
                        ChoiceId = option.ID,
                        Choice = option.Name,
                        Score = publicScore + inviteScore + bonusScore
                    });
                }

                apiVotes.Add(vote);
            }
            
            var apiEvent = new ApiEvent
            {
                Id = eventId,
                UserId = userId,
                Name = targetEvent.Name,
                Description = targetEvent.Description,
                StartDate = targetEvent.StartDate,
                EndDate = targetEvent.EndDate,
                Public = targetEvent.IsPublic,
                ShowOverallWinner = targetEvent.ShowOverallWinner,
                Votes = apiVotes
            };


            return apiEvent;
        }

    }

    public class ApiEvent
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public bool Public { get; set; }
        public bool ShowOverallWinner { get; set; }

        public List<ApiVote> Votes { get; set; }
        
    }

    public class ApiVote
    {
        public Guid QuestionId { get; set; }
        public string Question { get; set; }
        public List<ApiChoice> Choices { get; set; }
    }

    public class ApiChoice
    {
        public Guid ChoiceId { get; set; }
        public string Choice { get; set; }
        public double Score { get; set; }
    }
}