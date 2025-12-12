using AutoMapper;
using SupplyChain.Infrastructure.Dtos;
using SupplyChain.Infrastructure.Model;

namespace SupplyChain.Infrastructure.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Mapeia Employee para EmployeeDto
            CreateMap<Employee, EmployeeDTO>()
                .ForMember(dest => dest.ManagerName, opt => opt.MapFrom(src => src.Manager != null ? src.Manager.FirstName + " " + src.Manager.LastName : null));

            // Mapeia EmployeeCreateDto para Employee
            CreateMap<EmployeeCreateDto, Employee>();

            // Mapeia EmployeeUpdateDto para Employee
            CreateMap<EmployeeUpdateDto, Employee>();

            // Mapeia Role para RoleDto
            CreateMap<Role, RoleDto>();

            // Mapeia RoleCreateDto para Role
            CreateMap<RoleCreateDto, Role>();

            // Mapeia RoleUpdateDto para Role
            CreateMap<RoleUpdateDto, Role>();
        }
    }
}
