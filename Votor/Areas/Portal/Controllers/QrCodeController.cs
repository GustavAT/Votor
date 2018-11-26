using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Votor.Areas.Portal.Data;
using Votor.Areas.Portal.Models;

namespace Votor.Areas.Portal.Controllers
{
    [Area("Portal")]
    public class QrCodeController : Controller
    {

        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly VotorContext _context;

        public QrCodeController(IStringLocalizer<SharedResources> localizer,
            UserManager<IdentityUser> userManager,
            VotorContext context)
        {
            _localizer = localizer;
            _userManager = userManager;
            _context = context;
        }

        [Authorize]
        public async Task<IActionResult> Index(Guid eventId)
        {
            var targetEvent = await GetEventById(eventId);

            if (targetEvent == null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var model = new QrCodeModel
            {
                EventName = targetEvent.Name
            };

            if (targetEvent.IsPublic)
            {
                model.PublicQrCode = new QrCodeTokenModel
                {
                    Name = targetEvent.Name,
                    QrCode = EventController.GenerateVotingUrl(targetEvent.ID, HttpContext)
                };
            }

            foreach (var token in targetEvent.Tokens)
            {
                model.Tokens.Add(new  QrCodeTokenModel
                {
                    Name = token.Name,
                    QrCode = EventController.GenerateVotingUrl(token.ID, HttpContext)
                });
            }

            return View("Index", model);
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
                .Include(x => x.Tokens)
                .FirstOrDefault();
        }
    }

    public class QrCodeModel
    {
        public string EventName { get; set; }

        public QrCodeTokenModel PublicQrCode { get; set; }

        public List<QrCodeTokenModel> Tokens { get; set; } = new List<QrCodeTokenModel>();
    }

    public class QrCodeTokenModel
    {
        public string Name { get; set; }
        public string QrCode { get; set; }
    }
}