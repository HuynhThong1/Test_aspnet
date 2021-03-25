using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TCS2010PPTG4.Data;
using TCS2010PPTG4.Models;

namespace TCS2010PPTG4.Areas.Staff
{
    [Area("Staff")]
    public class ContributionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContributionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Staff/Contributions
        public async Task<IActionResult> Index(int topicId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FindAsync(userId);

            var contributions = await _context.Contribution.Include(c => c.Topic)
                                                           .Include(c => c.Contributor)
                                                           .Where(c => c.TopicId == topicId 
                                                                    && c.Contributor.DepartmentId == user.DepartmentId)
                                                           .ToArrayAsync();
            return View(contributions);
        }

        // GET: Staff/Contributions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contribution = await _context.Contribution
                .Include(c => c.Contributor)
                .Include(c => c.Topic)
                .Include(c => c.Files)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contribution == null)
            {
                return NotFound();
            }

            var comments = await _context.Comment.Include(c => c.User)
                                                .Where(c => c.ContributionId == id)
                                                .OrderBy(c => c.Date)
                                                .ToListAsync();

            ViewData["Comments"] = comments;

            return View(contribution);
        }

        // GET: Staff/Contributions/Create
        public IActionResult Create()
        {
            ViewData["ContributorId"] = new SelectList(_context.Users, "Id", "Id");
            ViewData["TopicId"] = new SelectList(_context.Topic, "Id", "Id");
            return View();
        }

        // POST: Staff/Contributions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Status,ContributorId,TopicId")] Contribution contribution)
        {
            if (ModelState.IsValid)
            {
                _context.Add(contribution);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ContributorId"] = new SelectList(_context.Users, "Id", "Id", contribution.ContributorId);
            ViewData["TopicId"] = new SelectList(_context.Topic, "Id", "Id", contribution.TopicId);
            return View(contribution);
        }

        // GET: Staff/Contributions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contribution = await _context.Contribution.FindAsync(id);
            if (contribution == null)
            {
                return NotFound();
            }
            ViewData["ContributorId"] = new SelectList(_context.Users, "Id", "Id", contribution.ContributorId);
            ViewData["TopicId"] = new SelectList(_context.Topic, "Id", "Id", contribution.TopicId);
            return View(contribution);
        }

        // POST: Staff/Contributions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Status,ContributorId,TopicId")] Contribution contribution)
        {
            if (id != contribution.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(contribution);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContributionExists(contribution.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ContributorId"] = new SelectList(_context.Users, "Id", "Id", contribution.ContributorId);
            ViewData["TopicId"] = new SelectList(_context.Topic, "Id", "Id", contribution.TopicId);
            return View(contribution);
        }

        // GET: Staff/Contributions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contribution = await _context.Contribution
                .Include(c => c.Contributor)
                .Include(c => c.Topic)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contribution == null)
            {
                return NotFound();
            }

            return View(contribution);
        }

        // POST: Staff/Contributions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contribution = await _context.Contribution.FindAsync(id);
            _context.Contribution.Remove(contribution);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ContributionExists(int id)
        {
            return _context.Contribution.Any(e => e.Id == id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Comment(int contributionId, string commentContent)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (ModelState.IsValid)
            {
                var user = await _context.Users.FindAsync(userId);
                var existContribution = await _context.Contribution.FindAsync(contributionId);

                if (existContribution != null && !String.IsNullOrEmpty(commentContent))
                {
                    var comment = new Comment();

                    comment.Content = commentContent;
                    comment.Date = DateTime.Now;
                    comment.ContributionId = existContribution.Id;
                    comment.UserId = userId;

                    _context.Add(comment);
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Details), new { id = contributionId });
        }
    }
}
