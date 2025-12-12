using SupplyChain.Infrastructure.Model;
using System;

namespace SupplyChain.Infrastructure.Model
{
    public class Log
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string ActionType { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Details { get; set; }

        // Chave estrangeira para a tabela Employee
        public virtual Employee Employee { get; set; }
    }
}

