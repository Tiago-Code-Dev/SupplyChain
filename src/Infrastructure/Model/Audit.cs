using SupplyChain.Infrastructure.Model;

namespace SupplyChain.Infrastructure.Model
{
    public class Audit
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string Field { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // Chave estrangeira para a tabela Employee
        public virtual Employee Employee { get; set; }
    }
}
