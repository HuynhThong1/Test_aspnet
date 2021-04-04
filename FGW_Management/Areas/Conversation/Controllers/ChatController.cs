using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FGW_Management.Data;
using FGW_Management.Models;
using System.Security.Claims;

namespace FGW_Management.Areas.Conversation
{
    [Area("Conversation")]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChatController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Conversation/Chat
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Chats.Include(c => c.User);

            List<Chat> chats = null;

            chats = await _context.Chats.Include(c => c.User)
                                                 .OrderBy(c => c.Date)
                                                 .ToListAsync();

            ViewData["Chats"] = chats;

            return View(await applicationDbContext.ToListAsync());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Chat(string chatContent)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (ModelState.IsValid)
            {
                var user = await _context.Users.FindAsync(userId);

                if (!String.IsNullOrEmpty(chatContent))
                {
                    var chat = new Chat();

                    chat.Content = chatContent;
                    chat.Date = DateTime.Now;
                    chat.UserId = userId;

                    _context.Add(chat);
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteChat(int chatId)
        {
            if (ModelState.IsValid)
            {

                var chatted = await _context.Chats.FindAsync(chatId);

                _context.Remove(chatted);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
        
}
