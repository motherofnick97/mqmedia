using Abp.Application.Services;
using MqSocial.Companies.Dto;
using System;

namespace MqSocial.Companies;

public interface ICompanyAppService : IAsyncCrudAppService<CompanyDto, Guid, PagedCompanyRequestDto, CreateCompanyDto, CompanyDto>
{
}
