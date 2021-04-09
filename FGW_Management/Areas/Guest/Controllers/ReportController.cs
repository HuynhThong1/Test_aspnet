using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using FGW_Management.Data;
using Microsoft.EntityFrameworkCore;

namespace FGW_Management.Areas.Guest.Controllers
{
    [Area("Guest")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(int departmentId = -1)
        {
            var studentIds = await _context.UserRoles.Where(u => u.RoleId == "Student").
                                                      Select(u => u.UserId)
                                                      .ToListAsync();

            var students = await _context.Users.Where(u => studentIds.Contains(u.Id)).ToListAsync();
            var studentofDepartment = students.Where(s => s.DepartmentId == departmentId).ToList();

            ViewData["TotalStudent"] = students.Count();
            ViewData["TotalStudentofDept"] = studentofDepartment.Count();
            ViewData["TotalDepartment"] = await _context.Departments.CountAsync();
            return View();
        }
    }
}
