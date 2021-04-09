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
using MimeKit;
using MailKit.Net.Smtp;
using System.IO.Compression;
using System.Net.Mime;
using Aspose.Words;

namespace FGW_Management.Areas.Coordinator.Views
{
    [Area("Coordinator")]
    public class ContributionsController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;

        public ContributionsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _env = env;
            _context = context;
        }

        // GET: Coordinator/Contributions
        public async Task<IActionResult> Index(int submissionId)
        {
            var contributions = await _context.Contributions.Include(c => c.Submission)
                                                           .Include(c => c.Contributor)
                                                           .Where(c => c.SubmissionId == submissionId)
                                                           .ToListAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var roleId = await _context.UserRoles.Where(u => u.UserId == userId)
                                           .Select(u => u.RoleId).FirstOrDefaultAsync();

            ViewData["TopicId"] = submissionId;

            if (contributions != null && roleId != null)
            {
                if (roleId == "Manager")
                {
                    contributions = contributions.Where(c => c.Status == ContributionStatus.Approved).ToList();
                    return View(contributions);
                }
                else if (roleId == "Coordinator")
                {
                    var user = await _context.Users.FindAsync(userId);
                    contributions = contributions.Where(c => c.Contributor.DepartmentId == user.DepartmentId).ToList();
                    return View(contributions);
                }
            }

            

            return View(null);
        }

        // GET: Coordinator/Contributions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contribution = await _context.Contributions
                .Include(c => c.Contributor)
                .Include(c => c.Submission)
                .Include(c => c.SubmittedFiles)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contribution == null)
            {
                return NotFound();
            }

            var comments = await _context.Comments.Include(c => c.User)
                                                .Where(c => c.ContributionId == id)
                                                .OrderBy(c => c.Date)
                                                .ToListAsync();
            //covert docx to pdf
            foreach (var file in contribution.SubmittedFiles)
            {
                // Load the document from disk.
                Document doc = new Document(file.URL);
                // Save as PDF
                var path = Path.Combine(_env.WebRootPath, _Global.PATH_TEMP, Path.GetFileNameWithoutExtension(file.URL) + ".pdf");
                doc.Save(path);

            }
            ViewData["Comments"] = comments;

            return View(contribution);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Comment(int contributionId, string commentContent)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (ModelState.IsValid)
            {
                var user = await _context.Users.FindAsync(userId);
                var existContribution = await _context.Contributions.FindAsync(contributionId);

                if (existContribution != null && !String.IsNullOrEmpty(commentContent))
                {
                    var comment = new Models.Comment();

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            int contributionId = 0;
            if (ModelState.IsValid)
            {

                var commented = await _context.Comments.FindAsync(commentId);
                var contribution = await _context.Contributions.FindAsync(commented.ContributionId);

                contributionId = contribution.Id;
                _context.Remove(commented);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = contributionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Mark(int contributionId, ContributionStatus contributionStatus)
        {

            if (ModelState.IsValid)
            {
                try
                {
                    var existContribution = await _context.Contributions.Include(c => c.Contributor).Include(c => c.Submission)
                                                                         .FirstOrDefaultAsync(c => c.Id == contributionId);


                    existContribution.Status = contributionStatus;

                    _context.Update(existContribution);
                    await _context.SaveChangesAsync();

                    var contributionFullname = $"{existContribution.Contributor.FirstName} {existContribution.Contributor.LastName}";

                    MailboxAddress from = new MailboxAddress("FGW Management System", "huynhmihthong1912@gmail.com");
                    MailboxAddress to = new MailboxAddress(contributionFullname, existContribution.Contributor.Email);

                    BodyBuilder bodyBuilder = new BodyBuilder();
                    bodyBuilder.TextBody = $"Hello {contributionFullname}, \n\n" +
                                           $"Your contribution for {existContribution.Submission.Title} is {contributionStatus}\n\n" + 
                                           $"Thank you for your contribution, \n\n" + 
                                           $"Best regards.";

                    MimeMessage message = new MimeMessage();
                    message.From.Add(from);
                    message.To.Add(to);
                    message.Subject = $"Contribution for {existContribution.Submission.Title} Status";
                    message.Body = bodyBuilder.ToMessageBody();

                    SmtpClient client = new SmtpClient();

                    client.Connect("smtp.gmail.com", 465, true);
                    client.Authenticate("huynhminhthong1912", "pqdquwmvialvcchv");

                    client.Send(message);
                    client.Disconnect(true);
                    client.Dispose();
                    
                }
                catch
                {

                }

            }

            return RedirectToAction(nameof(Details), new { id = contributionId });
        }

        public async Task<ActionResult> DownloadApprovedFile (int topicId = -1)
        {
            var approvedContributions = await _context.Contributions.Include(c => c.Contributor)
                                                                    .Include(c => c.SubmittedFiles)
                                                                    .Where(c => c.SubmissionId == topicId
                                                                    && c.Status == ContributionStatus.Approved).ToListAsync();

            if(approvedContributions.Count() > 0)
            {
                var topic = await _context.Submissions.FindAsync(topicId);
                var zipPath = Path.Combine(_Global.PATH_TOPIC, topicId.ToString(), topic.Title + ".zip");

                using (FileStream zipToOpen = new FileStream(zipPath, FileMode.Create))
                {
                    using (ZipArchive achive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                    {
                        foreach (var contribution in approvedContributions)
                        {
                            foreach (var file in contribution.SubmittedFiles)
                            {
                                achive.CreateEntryFromFile(file.URL, Path.Combine(contribution.Contributor.Number
                                                                                    , Path.GetFileName(file.URL))); 
                            }
                        }
                    }
                }

                byte[] fileBytes = System.IO.File.ReadAllBytes(zipPath);

                System.IO.File.Delete(zipPath);


                return File(fileBytes, MediaTypeNames.Application.Zip, Path.GetFileName(zipPath));
            }

            return NoContent();
        }

        public async Task<ActionResult> DownloadFile (int fileId = -1)
        {
            var file = await _context.SubmittedFiles.FindAsync(fileId);
            byte[] fileBytes = System.IO.File.ReadAllBytes(file.URL);
            return File(fileBytes, MediaTypeNames.Application.Octet, Path.GetFileName(file.URL));
        }
    }
}
