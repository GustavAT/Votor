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
        public async Task<IActionResult> Index(Guid eventId, string groupName)
        {
            var targetEvent = await GetEventById(eventId);

            if (targetEvent == null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var model = new QrCodeModel
            {
                EventName = targetEvent.Name,
                Tokens = new List<QrCodeTokenModel>()
            };

            if (targetEvent.IsPublic && string.IsNullOrEmpty(groupName))
            {
                var url = EventController.GenerateVotingUrl(targetEvent.ID, HttpContext);
                var qrCode = Util.ToImageSourceString(Util.GenerateQrCode(url));
                model.PublicQrCode = new QrCodeTokenModel
                {
                    Name = targetEvent.Name,
                    QrCode = qrCode
                };
            }

            var targetTokens = targetEvent.Tokens;

            if (!string.IsNullOrEmpty(groupName))
            {
                targetTokens = targetEvent.Tokens.Where(x => x.Name == groupName).ToList();
            }

            foreach (var token in targetTokens)
            {
                var url = EventController.GenerateVotingUrl(token.ID, HttpContext);
                var qrCode = Util.ToImageSourceString(Util.GenerateQrCode(url));
                model.Tokens.Add(new  QrCodeTokenModel
                {
                    Name = token.Name,
                    QrCode = qrCode
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