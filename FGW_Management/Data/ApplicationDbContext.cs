using FGW_Management.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FGW_Management.Data
{
    public class ApplicationDbContext : IdentityDbContext<FGW_User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Department> Departments { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<SubmittedFile> SubmittedFiles { get; set; }
        public DbSet<Contribution> Contributions { get; set; }
        public DbSet<Chat> Chats { get; set; }

    }
}
