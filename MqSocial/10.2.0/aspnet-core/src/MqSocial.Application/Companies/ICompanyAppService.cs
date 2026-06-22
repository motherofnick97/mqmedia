using Abp.Application.Services;
using MqSocial.Companies.Dto;

namespace MqSocial.Companies;

public interface ICompanyAppService : IAsyncCrudAppService<CompanyDto, int, PagedCompanyRequestDto, CreateCompanyDto, CompanyDto>
{
}
