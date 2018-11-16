using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        
        public EventController(VotorContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var editEvent = await InitEditEventModel(id);

            // Event not found or invalid user
            if (editEvent == null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View("Edit", editEvent);
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

            return View("Edit", await InitEditEventModel(eventId));
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EventModel eventModel)
        {
            //var eventModel = editEventModel.EventModel;
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

            return View("Edit", await InitEditEventModel(eventModel.Id));
        }

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
                Questions = GetQuestionsByEventId(eventId)
            };

            // todo: init questions, invite-links etc here

            return model;
        }
    }

    public class EditEventModel
    {
        public EventModel EventModel { get; set; }

        public List<QuestionModel> Questions { get; set; }
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
}
