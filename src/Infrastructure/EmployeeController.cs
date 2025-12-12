using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupplyChain.Infrastructure;
using SupplyChain.Infrastructure.Dtos;
using SupplyChain.Infrastructure.Model;

namespace SupplyChain.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    #region Injeções de dependência
    public class EmployeeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public EmployeeController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
    #endregion

        #region Cria um empregado
        [HttpPost]
        [EndpointSummary("Cria um empregado.")]
        public async Task<ActionResult<EmployeeDTO>> PostEmployee(EmployeeCreateDto employeeCreateDto)
        {
            var employee = _mapper.Map<Employee>(employeeCreateDto);

            if (!employee.IsAdult())
            {   
                return BadRequest("O empregado deve ser maior de idade.");
            }

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            var employeeDto = _mapper.Map<EmployeeDTO>(employee);
            return CreatedAtAction("GetEmployee", new { id = employee.Id }, employeeDto);
        }
        #endregion

        #region retorna todos os empregados
        [Authorize]
        [HttpGet]
        [EndpointSummary("Retorna os empregados.")]
        public async Task<ActionResult<IEnumerable<EmployeeDTO>>> GetEmployees()
        {
            var employees = await _context.Employees.ToListAsync();
            return Ok(_mapper.Map<IEnumerable<EmployeeDTO>>(employees));
        }
        #endregion

        #region retorna os empregados por ID
        [Authorize]
        [HttpGet("{id}")]
        [EndpointSummary("Retorna os empregados por ID.")]
        public async Task<ActionResult<EmployeeDTO>> GetEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);

            if (employee == null)
            {
                return NotFound();
            }

            return _mapper.Map<EmployeeDTO>(employee);
        }
        #endregion

        #region Atualiza um empregado
        [Authorize]
        [HttpPut("{id}")]
        [EndpointSummary("Atualiza um empregado.")]
        public async Task<IActionResult> PutEmployee(int id, EmployeeUpdateDto employeeUpdateDto)
        {
            var employee = await _context.Employees.FindAsync(id);
            
            if (employee == null)
            {
                return NotFound();
            }
            
            _mapper.Map(employeeUpdateDto, employee);

            if (!employee.IsAdult())
            {
                return BadRequest("O empregado deve ser maior de idade.");
            }

            _context.Entry(employee).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok("Usuário atualizado com sucesso.");
        }
        #endregion

        #region Deleta um empregado
        [Authorize]
        [HttpDelete("{id}")]
        [EndpointSummary("Deleta um empregado.")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);

            if (employee == null)
            {
                return NotFound();
            }

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            return Ok("Usuario deletado com sucesso.");
        }   
        #endregion
    }
}
