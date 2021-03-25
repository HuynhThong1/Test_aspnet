using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TCS2010PPTG4.Data;
using TCS2010PPTG4.Models;

namespace TCS2010PPTG4.Controllers
{
    public class TopicController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;

        public TopicController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _env = env;
            _context = context;
        }

        // GET: Topic
        public async Task<IActionResult> Index()
        {
            return View(await _context.Topic.ToListAsync());
        }

        // GET: Topic/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var topic = await _context.Topic
                .FirstOrDefaultAsync(m => m.Id == id);
            if (topic == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentContribution = await _context.Contribution.Include(c => c.Files).FirstOrDefaultAsync(c => c.ContributorId == userId 
                                                                                                       && c.TopicId == id);

            List<Comment> comments = null;
            if(currentContribution != null)
            {
                comments = await _context.Comment.Include(c => c.User)
                                                 .Where(c => c.ContributionId == currentContribution.Id)
                                                 .OrderBy(c => c.Date)
                                                 .ToListAsync();
            }

            ViewData["Comments"] = comments;
            ViewData["ContributorId"] = userId;
            ViewData["currentContribution"] = currentContribution;

            return View(topic);
        }

        // GET: Topic/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Topic/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Topic topic)
        {
            if (ModelState.IsValid)
            {
                _context.Add(topic);
                await _context.SaveChangesAsync();

                string webRootPath = _env.WebRootPath;
                var folderName = topic.Id.ToString();

                var path = Path.Combine(webRootPath, _Global.PATH_TOPIC, folderName);

                if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }

                return RedirectToAction(nameof(Index));
            }

            return View(topic);
        }

        // GET: Topic/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) { return NotFound(); }

            var topic = await _context.Topic.FindAsync(id);

            if (topic == null) { return NotFound(); }

            return View(topic);
        }

        // POST: Topic/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Deadline_1")] Topic topic)
        {
            if (id != topic.Id) { return NotFound(); }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(topic);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TopicExists(topic.Id)) { return NotFound(); }
                    else { throw; }
                }

                return RedirectToAction(nameof(Index));
            }

            return View(topic);
        }

        // GET: Topic/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var topic = await _context.Topic.FirstOrDefaultAsync(m => m.Id == id);

            if (topic == null)
            {
                return NotFound();
            }

            return View(topic);
        }

        // POST: Topic/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var topic = await _context.Topic.FindAsync(id);
            _context.Topic.Remove(topic);
            await _context.SaveChangesAsync();

            string webRootPath = _env.WebRootPath;
            var folderName = id.ToString();

            var path = Path.Combine(webRootPath, _Global.PATH_TOPIC, folderName);

            if (Directory.Exists(path)) { Directory.Delete(path); }

            return RedirectToAction(nameof(Index));
        }

        private bool TopicExists(int id)
        {
            return _context.Topic.Any(e => e.Id == id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(Contribution contribution, IFormFile file)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (ModelState.IsValid)
            {
                var user = await _context.Users.FindAsync(userId);
                var existContribution = await _context.Contribution.FirstOrDefaultAsync(c => c.ContributorId == userId && c.TopicId == contribution.TopicId);

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

                        //create folder
                        string webRootPath = _env.WebRootPath;
                        var path = Path.Combine(webRootPath, _Global.PATH_TOPIC, existContribution.TopicId.ToString(), user.Number);
                        if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
                        // Upload file, create file
                        path = Path.Combine(path, String.Format("{0}.{1:yyyy-MM-dd.ss-mm-HH}{2}", user.Number, DateTime.Now, fileExtension));
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

            return RedirectToAction(nameof(Details), new { id = contribution.TopicId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Comment (int topicId, string commentContent)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (ModelState.IsValid)
            {
                var user = await _context.Users.FindAsync(userId);
                var existContribution = await _context.Contribution.FirstOrDefaultAsync(c => c.ContributorId == userId 
                                                                                        && c.TopicId == topicId);

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
    }
}
