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

namespace FGW_Management.Areas.Manager.Views
{
    [Area("Manager")]
    public class ContributionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContributionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Manager/Contributions
        public async Task<IActionResult> Index(int submissionId)
        {
            var contributions = await _context.Contributions.Include(c => c.Submission)
                                                           .Include(c => c.Contributor)
                                                           .Where(c => c.SubmissionId == submissionId)
                                                           .ToListAsync();

            ViewData["TopicId"] = submissionId;
            contributions = contributions.Where(c => c.Status == ContributionStatus.Approved).ToList();
            return View(contributions);

        }

        // GET: Manager/Contributions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contribution = await _context.Contributions
                .Include(c => c.Contributor)
                .Include(c => c.Submission)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contribution == null)
            {
                return NotFound();
            }

            return View(contribution);
        }

        private bool ContributionExists(int id)
        {
            return _context.Contributions.Any(e => e.Id == id);
        }
    }
}
