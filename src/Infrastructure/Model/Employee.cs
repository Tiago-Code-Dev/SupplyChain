using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupplyChain.Infrastructure.Model
{
    public class Employee
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; }

        [Required]
        [StringLength(20)]
        public string DocumentNumber { get; set; }

        [Required]
        [Phone]
        public string Phone { get; set; }


        // Relacionamento com o gerente (outro funcionário)
        public int? ManagerId { get; set; }

        [ForeignKey("ManagerId")]
        public virtual Employee Manager { get; set; }

        [Required(ErrorMessage = "Campo Obrigatório")]
        [StringLength(256)]
        public string Password { get; set; }

        public bool IsAdult()
        {
            var age = DateTime.Now.Year - DateOfBirth.Year;
            if (DateTime.Now < DateOfBirth.AddYears(age)) age--;
            return age >= 18;
        }
        [Display(Name = "Data de nascimento")]
        [Column(TypeName = "date")]
        public DateTime DateOfBirth { get; set; } 
    }
}
