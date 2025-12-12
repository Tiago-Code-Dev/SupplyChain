using System.ComponentModel.DataAnnotations;

namespace SupplyChain.Infrastructure.Model
{
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string RoleName { get; set; }
    }
}
