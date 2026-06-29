using Abp.Application.Services;
using MqSocial.ContractKolReviews.Dto;
using System;

namespace MqSocial.ContractKolReviews;

public interface IContractKolReviewAppService
    : IAsyncCrudAppService<ContractKolReviewDto, Guid, PagedContractKolReviewRequestDto, CreateContractKolReviewDto, ContractKolReviewDto>
{
}
