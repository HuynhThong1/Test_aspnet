using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FGW_Management.Data;
using FGW_Management.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace FGW_Management.Areas.Admin.Views
{
    [Area("Admin")]
    public class SubmissionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SubmissionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Submissions
        public async Task<IActionResult> Index()
        {
            return View(await _context.Submissions.ToListAsync());
        }

        // GET: Admin/Submissions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var submission = await _context.Submissions
                .FirstOrDefaultAsync(m => m.Id == id);
            if (submission == null)
            {
                return NotFound();
            }

            return View(submission);
        }

        // GET: Admin/Submissions/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Submissions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Submission submission)
        {
            if (ModelState.IsValid)
            {
                if (submission.SubmissionDeadline_1 <= submission.SubmissionDeadline_2)
                {
                    submission.CreationDay = DateTime.Now;

                    _context.Add(submission);
                    await _context.SaveChangesAsync();

                    var folderName = submission.Id.ToString();

                    var path = Path.Combine( _Global.PATH_TOPIC, folderName);

                    if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }

                    return RedirectToAction(nameof(Index));
                }
                ViewData["Error"] = "Deadline 2 is not acceptable.";
            }

            return View(submission);
        }

        // GET: Admin/Submissions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var submission = await _context.Submissions.FindAsync(id);
            if (submission == null)
            {
                return NotFound();
            }
            return View(submission);
        }

        // POST: Admin/Submissions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,SubmissionDue,SubmissionDeadline_1,SubmissionDeadline_2")] Submission submission)
        {
            if (id != submission.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                if (submission.SubmissionDeadline_1 <= submission.SubmissionDeadline_2)
                {
                    try
                    {
                        _context.Update(submission);
                        await _context.SaveChangesAsync();

                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!SubmissionExists(submission.Id))
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
                ViewData["Error"] = "Deadline 2 is not acceptable.";
            }
            return View(submission);
        }

        // GET: Admin/Submissions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var submission = await _context.Submissions
                .FirstOrDefaultAsync(m => m.Id == id);
            if (submission == null)
            {
                return NotFound();
            }

            return View(submission);
        }

        // POST: Admin/Submissions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var submission = await _context.Submissions.FindAsync(id);
            _context.Submissions.Remove(submission);
            await _context.SaveChangesAsync();

            var folderName = id.ToString();

            var path = Path.Combine(_Global.PATH_TOPIC, folderName);

            if (Directory.Exists(path)) { Directory.Delete(path); }

            return RedirectToAction(nameof(Index));
        }

        private bool SubmissionExists(int id)
        {
            return _context.Submissions.Any(e => e.Id == id);
        }
    }
}
