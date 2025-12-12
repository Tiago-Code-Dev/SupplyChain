namespace SupplyChain.Infrastructure.Dtos
{
    public class EmployeeDTO
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string DocumentNumber { get; set; }
        public string Phone { get; set; }
        public string ManagerName { get; set; }
        public DateTime DateOfBirth { get; set; }
    }
}
