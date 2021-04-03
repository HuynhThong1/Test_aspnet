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
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Net.Mime;
using MimeKit;
using MailKit.Net.Smtp;

namespace FGW_Management.Areas.Student.Views
{
    [Area("Student")]
    public class SubmissionsController : Controller
    {
        
        private readonly ApplicationDbContext _context;

        public SubmissionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Student/Submissions
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var topicIds = await _context.Contributions.Where(c => c.ContributorId == userId)
                                                           .Select(c => c.SubmissionId)
                                                           .ToListAsync();



            var topics = await _context.Submissions.Where(t => t.SubmissionDeadline_2 >= DateTime.Now || topicIds.Contains(t.Id))
                                             .OrderByDescending(t => t.SubmissionDeadline_2)
                                             .ToListAsync();

            return View(topics);
        }

        // GET: Student/Submissions/Details/5
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


            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentContribution = await _context.Contributions.Include(c => c.SubmittedFiles).FirstOrDefaultAsync(c => c.ContributorId == userId
                                                                                                       && c.SubmissionId == id);

            List<Comment> comments = null;
            if (currentContribution != null)
            {
                comments = await _context.Comments.Include(c => c.User)
                                                 .Where(c => c.ContributionId == currentContribution.Id)
                                                 .OrderBy(c => c.Date)
                                                 .ToListAsync();
            }

            ViewData["Comments"] = comments;
            ViewData["ContributorId"] = userId;
            ViewData["currentContribution"] = currentContribution;

            return View(submission);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(Contribution contribution, IFormFile file, ContributionStatus contributionStatus, int contributionId)
        {
            var topic = await _context.Submissions.FirstOrDefaultAsync(t => t.Id == contribution.SubmissionId);

            if (topic.SubmissionDeadline_2 >= DateTime.Now)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (ModelState.IsValid)
                {
                    var user = await _context.Users.FindAsync(userId);
                    var existContribution = await _context.Contributions.FirstOrDefaultAsync(c => c.ContributorId == userId && c.SubmissionId == contribution.SubmissionId);

                    if (existContribution == null)
                    {
                        contribution.Status = ContributionStatus.Pending;

                        _context.Add(contribution);
                        await _context.SaveChangesAsync();

                        existContribution = contribution;
                    }

                    else
                    {
                        existContribution.Status = ContributionStatus.Pending;

                        _context.Update(existContribution);
                        await _context.SaveChangesAsync();
                    }

                    if (file.Length > 0)
                    {
                        FileType? fileType;
                        string fileExtension = Path.GetExtension(file.FileName).ToLower();

                        switch (fileExtension)
                        {
                            case ".doc": case ".docx": fileType = FileType.Document; break;
                            case ".jpg": case ".png": fileType = FileType.Image; break;
                            default: fileType = null; break;
                        }

                        if (fileType != null)
                        {
                            var path = Path.Combine(_Global.PATH_TOPIC, existContribution.SubmissionId.ToString(), user.Number);
                            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
                            // Upload file, create file
                            var contributionDate = DateTime.Now;
                            path = Path.Combine(path, String.Format("{0}.{1:yyyy-MM-dd.ss-mm-HH}{2}", user.Number, contributionDate, fileExtension));
                            var stream = new FileStream(path, FileMode.Create);
                            file.CopyTo(stream);
                            var newFile = new SubmittedFile();
                            newFile.ContributionId = existContribution.Id;
                            newFile.URL = path;
                            newFile.Type = (FileType)fileType;
                            _context.Add(newFile);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }
            else
            {

            }
            return RedirectToAction(nameof(Details), new { id = contribution.SubmissionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUpload(Contribution contribution, int fileId)
        {
            var topic = await _context.Submissions.FirstOrDefaultAsync(t => t.Id == contribution.SubmissionId);

            if (topic.SubmissionDeadline_2 >= DateTime.Now)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (ModelState.IsValid)
                {

                    var fileSubmitted = await _context.SubmittedFiles.FindAsync(fileId);
                    System.IO.File.Delete(fileSubmitted.URL);

                    _context.Remove(fileSubmitted);
                    await _context.SaveChangesAsync();
                }
               
            }
            else
            {

            }
            return RedirectToAction(nameof(Details), new { id = contribution.SubmissionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Comment(int topicId, string commentContent)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (ModelState.IsValid)
            {
                var user = await _context.Users.FindAsync(userId);
                var existContribution = await _context.Contributions.FirstOrDefaultAsync(c => c.ContributorId == userId
                                                                                        && c.SubmissionId == topicId);

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

            return RedirectToAction(nameof(Details), new { id = topicId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            int topicId = 0;
            if (ModelState.IsValid)
            {

                var commented = await _context.Comments.FindAsync(commentId);
                var contribution = await _context.Contributions.FindAsync(commented.ContributionId);

                topicId = contribution.SubmissionId;
                _context.Remove(commented);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = topicId });
        }


        public async Task<ActionResult> DownloadFile(int fileId = -1)
        {
            var file = await _context.SubmittedFiles.FindAsync(fileId);
            byte[] fileBytes = System.IO.File.ReadAllBytes(file.URL);
            return File(fileBytes, MediaTypeNames.Application.Octet, Path.GetFileName(file.URL));
        }
    }
}
