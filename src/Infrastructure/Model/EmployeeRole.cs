using SupplyChain.Infrastructure.Model;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace SupplyChain.Infrastructure.Model
{
    public class EmployeeRole
    {
        [Key]
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public int RoleId { get; set; }

        // Chaves estrangeiras
        public virtual Employee Employee { get; set; }
        public virtual Role Role { get; set; }
    }
}
