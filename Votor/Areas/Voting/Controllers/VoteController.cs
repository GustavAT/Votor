using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public IActionResult Index(Guid id)
        {
            var targetEvent = GetActiveEventById(id);
            Token token;

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
            }
            else
            {
                // public vote
            }

            return View("Index");
        }

        public Event GetActiveEventById(Guid eventId)
        {
            return _context.Events
                .Where(x => x.ID == eventId && x.IsPublic
                                            && x.StartDate.HasValue
                                            && !x.EndDate.HasValue)
                .AsNoTracking().FirstOrDefault();
        }

        public Token GetTokenById(Guid tokenId)
        {
            return _context.Tokens.Where(x => x.ID == tokenId).AsNoTracking().FirstOrDefault();
        }
    }
}