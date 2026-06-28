using Abp.Application.Services;
using MqSocial.KolDuplicateContracts.Dto;
using System;

namespace MqSocial.KolDuplicateContracts;

public interface IKolDuplicateContractAppService
    : IAsyncCrudAppService<KolDuplicateContractDto, Guid, PagedKolDuplicateContractRequestDto, CreateKolDuplicateContractDto, KolDuplicateContractDto>
{
}
