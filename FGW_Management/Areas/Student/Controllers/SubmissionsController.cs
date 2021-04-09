using Aspose.Words;
using FGW_Management.Data;
using FGW_Management.Models;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FGW_Management.Areas.Student.Views
{
    [Area("Student")]
    public class SubmissionsController : Controller
    {

        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;
        bool privacyChecked = true;

        public SubmissionsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _env = env;
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

            List<Models.Comment> comments = null;
            if (currentContribution != null)
            {
                comments = await _context.Comments.Include(c => c.User)
                                                 .Where(c => c.ContributionId == currentContribution.Id)
                                                 .OrderBy(c => c.Date)
                                                 .ToListAsync();
            }

            if (!privacyChecked)
            {
                ViewData["Error"] = "Please accept all our privacy to submit your contribution.";
            }

            ViewData["Comments"] = comments;
            ViewData["ContributorId"] = userId;
            ViewData["currentContribution"] = currentContribution;

            return View(submission);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(Contribution contribution, IFormFile file,
                                                ContributionStatus contributionStatus, int contributionId, bool[] privacy)
        {
            if (privacy.Length == 3)
            {
                var topic = await _context.Submissions.FirstOrDefaultAsync(t => t.Id == contribution.SubmissionId);

                if (topic.SubmissionDeadline_2 >= DateTime.Now)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);


                    if (ModelState.IsValid)
                    {
                        var user = await _context.Users.FindAsync(userId);
                        var coordinatorIds = await _context.UserRoles.Where(u => u.RoleId == "Coordinator")
                                                                     .Select(u => u.UserId)
                                                                     .ToListAsync();

                        var coordinators = await _context.Users.Where(u => u.DepartmentId == user.DepartmentId
                                                                   && coordinatorIds.Contains(u.Id)).ToListAsync();
                        var existContribution = await _context.Contributions.FirstOrDefaultAsync(c => c.ContributorId == userId
                                                                                              && c.SubmissionId == contribution.SubmissionId);




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
                                try
                                {
                                    var path = Path.Combine(_Global.PATH_TOPIC, existContribution.SubmissionId.ToString(), user.Number);
                                    if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
                                    // Upload file, create file
                                    var contributionDate = DateTime.Now;
                                    path = Path.Combine(path, String.Format("{0}.{1:yyyy-MM-dd.ss-mm-HH}{2}", user.Number, contributionDate, fileExtension));
                                    using var stream = new FileStream(path, FileMode.Create);
                                    file.CopyTo(stream);
                                    var newFile = new SubmittedFile();
                                    newFile.ContributionId = existContribution.Id;
                                    newFile.URL = path;
                                    newFile.Type = (FileType)fileType;
                                    _context.Add(newFile);
                                    await _context.SaveChangesAsync();

                                    if (coordinators.Count() > 0)
                                    {
                                        foreach (var coordinator in coordinators)
                                        {
                                            var contributionFullname = $"{existContribution.Contributor.FirstName} {existContribution.Contributor.LastName}";
                                            var coordinatorFullName = $"{coordinator.FirstName} {coordinator.LastName}";

                                            MailboxAddress from = new MailboxAddress("FGW Management System", "huynhmihthong1912@gmail.com");
                                            MailboxAddress to = new MailboxAddress(coordinatorFullName, coordinator.Email);

                                            BodyBuilder bodyBuilder = new BodyBuilder();
                                            bodyBuilder.TextBody = $"Hello Coordinator,\n\n" +
                                                   $"Your student was submited thier contribution with the title is {existContribution.Submission.Title},\n\n" +
                                                   $"This is contribution by {contributionFullname},\n\n" +
                                                   $"Please review and give their feedback soon as possible {contributionFullname},\n\n" +
                                                   $"Thank you for checking this notification,\n\n" +
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
                                    }

                                }
                                catch
                                {

                                }
                            }
                        }


                    }
                }
            }
            else
            {
                TempData["PrivacyError"] = "Please accept all our privacy to submit your contribution.";
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

                    var path = Path.Combine(_env.WebRootPath, _Global.PATH_TEMP, Path.GetFileNameWithoutExtension(fileSubmitted.URL) + ".pdf");
                    System.IO.File.Delete(path);

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
                    var comment = new Models.Comment();

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
