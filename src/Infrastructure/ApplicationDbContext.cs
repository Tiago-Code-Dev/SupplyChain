using Microsoft.EntityFrameworkCore;
using SupplyChain.Infrastructure.Model;
using System.Collections.Generic;

namespace SupplyChain.Infrastructure
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<EmployeeRole> EmployeeRoles { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<Audit> Audits { get; set; }
    }
}
