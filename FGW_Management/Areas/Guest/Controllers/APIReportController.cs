using FGW_Management.Data;
using FGW_Management.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FGW_Management.Areas.Guest.Controllers
{
    [Route("api/report")]
    [ApiController]
    public class APIReportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public APIReportController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet("manager_contribution")]
        [Produces("application/json")]
        public async Task<IActionResult> API_Department_Contribution()
        {
            try
            {

                var currentYear = DateTime.Now.Year;
                var topicsIds = await _context.Submissions.Where(s => s.CreationDay.Year == currentYear)
                                                          .Select(s => s.Id)
                                                          .ToListAsync();
                var contributions = await _context.Contributions.Where(c => topicsIds.Contains(c.SubmissionId)).ToListAsync();

                List<API_Department_Contribution> statistics = new List<API_Department_Contribution>();
                foreach (var department in await _context.Departments.ToListAsync())
                {
                    var contributorIds = await _context.Users.Where(u => u.DepartmentId == department.Id)
                                                             .Select(u => u.Id)
                                                             .ToListAsync();
                    var totalContribution = contributions.Where(c => contributorIds.Contains(c.ContributorId))
                                                         .Count();

                    var temp = new API_Department_Contribution()
                    {
                        DepartmentName = department.Name,
                        TotalContribution = totalContribution
                    };
                    statistics.Add(temp);
                }
                return Ok(statistics);
            }
            catch
            {
                return BadRequest();
            }
        }
    }
}
