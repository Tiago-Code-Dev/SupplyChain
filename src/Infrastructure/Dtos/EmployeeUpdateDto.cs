namespace SupplyChain.Infrastructure.Dtos
{
    public class EmployeeUpdateDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string DocumentNumber { get; set; }
        public string Phone { get; set; }
        public int? ManagerId { get; set; }
        public DateTime DateOfBirth { get; set; }
    }
}
