using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Linq.Extensions;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using MqSocial.Authorization;
using MqSocial.Common.Enum;
using MqSocial.Contracts;
using MqSocial.ContractKolResults;
using MqSocial.ContractKolResults.Dto;
using MqSocial.ContractKols.Dto;
using MqSocial.ContractKolReviews;
using MqSocial.Emails;
using MqSocial.Emails.Dto;
using MqSocial.KolDuplicateContracts;
using MqSocial.Kols;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MqSocial.ContractKols;

[AbpAuthorize(PermissionNames.Pages_ContractKols)]
public class ContractKolAppService : AsyncCrudAppService<ContractKol, ContractKolDto, Guid, PagedContractKolRequestDto, CreateContractKolDto, ContractKolDto>, IContractKolAppService
{
    private static readonly Dictionary<ReceiveStatus, string> ReceiveStatusLabels = new()
    {
        [ReceiveStatus.NotShip] = "Chưa gửi",
        [ReceiveStatus.Shipping] = "Đang gửi",
        [ReceiveStatus.Received] = "Đã nhận",
        [ReceiveStatus.NotReceived] = "Không nhận",
    };

    private static readonly Dictionary<ChannelType, string> ChannelLabels = new()
    {
        [ChannelType.Tiktok] = "TikTok",
        [ChannelType.Facebook] = "Facebook",
        [ChannelType.Khac] = "Khác",
    };

    private readonly IRepository<Contract, Guid> _contractRepository;
    private readonly IRepository<KolDuplicateContract, Guid> _kolDuplicateContractRepository;
    private readonly IRepository<ContractKolReview, Guid> _contractKolReviewRepository;
    private readonly IRepository<Kol, Guid> _kolRepository;
    private readonly IRepository<ContractKolResult, Guid> _contractKolResultRepository;
    private readonly IEmailAppService _emailAppService;

    public ContractKolAppService(
        IRepository<ContractKol, Guid> repository,
        IRepository<KolDuplicateContract, Guid> kolDuplicateContractRepository,
        IRepository<Contract, Guid> contractRepository,
        IRepository<ContractKolReview, Guid> contractKolReviewRepository,
        IRepository<Kol, Guid> kolRepository,
        IRepository<ContractKolResult, Guid> contractKolResultRepository,
        IEmailAppService emailAppService)
        : base(repository)
    {
        _kolDuplicateContractRepository = kolDuplicateContractRepository;
        _contractRepository = contractRepository;
        _contractKolReviewRepository = contractKolReviewRepository;
        _kolRepository = kolRepository;
        _contractKolResultRepository = contractKolResultRepository;
        _emailAppService = emailAppService;
        CreatePermissionName = PermissionNames.Pages_ContractKols_Create;
        UpdatePermissionName = PermissionNames.Pages_ContractKols_Update;
        DeletePermissionName = PermissionNames.Pages_ContractKols_Delete;
    }

    protected override ContractKol MapToEntity(CreateContractKolDto createInput)
    {
        var entity = base.MapToEntity(createInput);
        entity.TenantId = AbpSession.TenantId;
        return entity;
    }

    protected override IQueryable<ContractKol> CreateFilteredQuery(PagedContractKolRequestDto input)
    {
        // Không Include(x => x.Kol) ở đây: Kol có thể có TenantId null (dữ liệu cũ) hoặc TenantId thật
        // (dữ liệu mới). Vì KolId là khóa ngoại bắt buộc, EF Core sẽ áp global tenant-filter của Kol lên
        // cả navigation Include và loại luôn dòng ContractKol nếu Kol không khớp tenant hiện tại — làm
        // mất hết kết quả tìm kiếm. KolName được nạp riêng (tắt hẳn tenant-filter) trong GetAllAsync bên dưới.
        return Repository.GetAll()
            .Include(x => x.Contract)
            .WhereIf(input.KolId.HasValue, x => x.KolId == input.KolId.Value)
            .WhereIf(input.ContractId.HasValue, x => x.ContractId == input.ContractId.Value)
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status.Value);
    }

    protected override IQueryable<ContractKol> ApplySorting(IQueryable<ContractKol> query, PagedContractKolRequestDto input)
    {
        return query.OrderBy(input.Sorting);
    }

    public override async Task<PagedResultDto<ContractKolDto>> GetAllAsync(PagedContractKolRequestDto input)
    {
        var result = await base.GetAllAsync(input);

        var kolIds = result.Items.Select(x => x.KolId).Distinct().ToList();
        if (kolIds.Count > 0)
        {
            Dictionary<Guid, (string Name, Guid? KolGeneralId)> kolInfos;
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                kolInfos = await _kolRepository.GetAll()
                    .Where(x => kolIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => new ValueTuple<string, Guid?>(x.Name, x.KolGeneralId));
            }

            foreach (var item in result.Items)
            {
                if (kolInfos.TryGetValue(item.KolId, out var kolInfo))
                {
                    item.KolName = kolInfo.Name;
                    item.KolGeneralId = kolInfo.KolGeneralId;
                }
                else
                {
                    item.KolName = null;
                    item.KolGeneralId = null;
                }
            }
        }

        if (!await IsPaymentGrantedAsync())
        {
            foreach (var item in result.Items)
            {
                item.Payment = 0;
            }
        }

        var contractKolIds = result.Items.Select(x => x.Id).ToList();
        if (contractKolIds.Count > 0)
        {
            var allResults = await _contractKolResultRepository.GetAll()
                .Where(x => contractKolIds.Contains(x.ContractKolId))
                .OrderBy(x => x.PostTime)
                .ToListAsync();

            var resultsByContractKolId = allResults
                .GroupBy(x => x.ContractKolId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var item in result.Items)
            {
                item.Results = resultsByContractKolId.TryGetValue(item.Id, out var kolResults)
                    ? ObjectMapper.Map<List<ContractKolResultDto>>(kolResults)
                    : new List<ContractKolResultDto>();
            }
        }

        return result;
    }

    public override async Task<ContractKolDto> GetAsync(EntityDto<Guid> input)
    {
        var result = await base.GetAsync(input);

        if (!await IsPaymentGrantedAsync())
        {
            result.Payment = 0;
        }

        return result;
    }

    private Task<bool> IsPaymentGrantedAsync()
    {
        return PermissionChecker.IsGrantedAsync(PermissionNames.Pages_ContractKols_Payment);
    }

    public override async Task<ContractKolDto> CreateAsync(CreateContractKolDto input)
    {
        var contract = await _contractRepository.GetAsync(input.ContractId);
        if (contract.Status == ContractStatus.Complete)
            throw new UserFriendlyException("Hợp đồng đã hoàn thành, không thể thêm KOL.");

        var existing = await Repository.GetAll()
            .FirstOrDefaultAsync(x => x.ContractId == input.ContractId && x.KolId == input.KolId);

        if (existing != null)
            throw new UserFriendlyException($"KOL đã được thêm vào hợp đồng trước đó");

        var conflictingContracts = await GetConflictingContractNamesAsync(input.ContractId, input.KolId);
        if (conflictingContracts.Count > 0)
        {
            var names = string.Join(", ", conflictingContracts);
            throw new UserFriendlyException($"KOL đã nằm trong hợp đồng không được phép trùng: {names}");
        }

        if (!await IsPaymentGrantedAsync())
        {
            input.Payment = 0;
        }

        return await base.CreateAsync(input);
    }

    public override async Task<ContractKolDto> UpdateAsync(ContractKolDto input)
    {
        var existing = await Repository.GetAsync(input.Id);

        if (existing.Status == ContractKolStatus.Done)
            throw new UserFriendlyException("Hợp đồng đã hoàn thành, không thể chỉnh sửa.");

        var oldReviewResult = existing.ReviewResult;

        if (!await IsPaymentGrantedAsync())
        {
            input.Payment = existing.Payment;
        }

        var isNewReview = !string.IsNullOrWhiteSpace(input.ReviewResult) && input.ReviewResult != oldReviewResult;

        if (isNewReview)
        {
            var contract = await _contractRepository.GetAsync(existing.ContractId);
            var reviewCount = await _contractKolReviewRepository.CountAsync(x => x.ContractKolId == input.Id);

            if (reviewCount >= contract.MaxReviewTime)
                throw new UserFriendlyException($"KOL này đã đạt giới hạn {contract.MaxReviewTime} lần review của hợp đồng.");
        }

        var result = await base.UpdateAsync(input);

        if (isNewReview)
        {
            await _contractKolReviewRepository.InsertAsync(new ContractKolReview
            {
                ContractKolId = input.Id,
                Review = input.ReviewResult,
                TenantId = AbpSession.TenantId,
            });
        }

        return result;
    }

    public override async Task DeleteAsync(EntityDto<Guid> input)
    {
        var existing = await Repository.GetAsync(input.Id);

        if (existing.Status == ContractKolStatus.Done)
            throw new UserFriendlyException("Hợp đồng đã hoàn thành, không thể xóa KOL.");

        await base.DeleteAsync(input);
    }

    private async Task<List<string>> GetConflictingContractNamesAsync(Guid contractId, Guid kolId)
    {
        var duplicateContractIds = await _kolDuplicateContractRepository.GetAll()
            .Where(x => x.FirstContractId == contractId || x.SecondContractId == contractId)
            .Select(x => x.FirstContractId == contractId ? x.SecondContractId : x.FirstContractId)
            .ToListAsync();

        if (!duplicateContractIds.Any())
            return new List<string>();

        var conflictingContractIds = await Repository.GetAll()
            .Where(x => x.KolId == kolId && duplicateContractIds.Contains(x.ContractId))
            .Select(x => x.ContractId)
            .Distinct()
            .ToListAsync();

        if (!conflictingContractIds.Any())
            return new List<string>();

        return await _contractRepository.GetAll()
            .Where(x => conflictingContractIds.Contains(x.Id))
            .Select(x => x.Name)
            .ToListAsync();
    }

    public async Task SendListEmailAsync(SendContractKolsEmailDto input)
    {
        if (input.To == null || input.To.Count == 0)
            throw new UserFriendlyException("Vui lòng nhập ít nhất một địa chỉ email.");

        var kolsResult = await GetAllAsync(new PagedContractKolRequestDto
        {
            ContractId = input.ContractId,
            Status = input.Status,
            Sorting = "Id",
            SkipCount = 0,
            MaxResultCount = 10000,
        });

        var kols = kolsResult.Items.ToList();
        var kolIds = kols.Select(x => x.Id).ToList();

        var results = await _contractKolResultRepository.GetAll()
            .Where(x => kolIds.Contains(x.ContractKolId))
            .OrderBy(x => x.PostTime)
            .ToListAsync();

        var resultsByKolId = results
            .GroupBy(x => x.ContractKolId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var html = BuildEmailHtml(input.Subject, input.Body, kols, resultsByKolId);

        await _emailAppService.SendAsync(new SendEmailDto
        {
            To = input.To,
            Subject = input.Subject,
            Body = html,
            IsBodyHtml = true,
        });
    }

    private static string BuildEmailHtml(
        string subject,
        string introBody,
        List<ContractKolDto> kols,
        Dictionary<Guid, List<ContractKolResult>> resultsByKolId)
    {
        static string Th(string t) => $"<th style=\"border:1px solid #ccc;padding:6px 10px;background:#f0f0f0;white-space:nowrap\">{t}</th>";
        static string Td(string t) => $"<td style=\"border:1px solid #ccc;padding:5px 10px;vertical-align:top\">{(string.IsNullOrEmpty(t) ? "—" : t)}</td>";

        var sb = new StringBuilder();
        sb.Append("<html><body style=\"font-family:Arial,sans-serif;font-size:13px\">");
        sb.Append($"<h2 style=\"color:#333\">{WebUtility.HtmlEncode(subject)}</h2>");
        if (!string.IsNullOrWhiteSpace(introBody))
            sb.Append($"<p style=\"white-space:pre-wrap\">{WebUtility.HtmlEncode(introBody)}</p>");

        sb.Append("<table style=\"border-collapse:collapse;width:100%;font-size:12px\">");
        sb.Append("<thead><tr>");
        sb.Append(Th("KOL") + Th("Hợp đồng") + Th("Trạng thái") + Th("Cash") + Th("Air Time") + Th("Nhận mẫu") + Th("Kết quả review"));
        sb.Append("</tr></thead><tbody>");

        foreach (var kol in kols)
        {
            sb.Append("<tr>");
            sb.Append(Td(WebUtility.HtmlEncode(kol.KolName)));
            sb.Append(Td(WebUtility.HtmlEncode(kol.ContractName)));
            sb.Append(Td(WebUtility.HtmlEncode(GetEnumDescription(kol.Status))));
            sb.Append(Td(kol.Cash.ToString("N0", CultureInfo.InvariantCulture)));
            sb.Append(Td(kol.AirTime.HasValue ? kol.AirTime.Value.ToString("dd/MM/yyyy") : null));
            sb.Append(Td(ReceiveStatusLabels.TryGetValue(kol.SampleReceiveStatus, out var receiveLabel) ? receiveLabel : null));
            sb.Append(Td(WebUtility.HtmlEncode(kol.ReviewResult)));
            sb.Append("</tr>");

            if (resultsByKolId.TryGetValue(kol.Id, out var kolResults) && kolResults.Count > 0)
            {
                sb.Append("<tr><td colspan=\"7\" style=\"padding:6px 10px 14px 24px;background:#fafafa;border:1px solid #ccc\">");
                sb.Append("<table style=\"border-collapse:collapse;width:100%;font-size:11px\">");
                sb.Append("<thead><tr>");
                sb.Append(Th("Ngày đăng") + Th("Kênh") + Th("Link bài") + Th("View") + Th("Comment") + Th("Like") + Th("Save") + Th("Share"));
                sb.Append("</tr></thead><tbody>");

                foreach (var r in kolResults)
                {
                    var link = string.IsNullOrWhiteSpace(r.PostLink)
                        ? null
                        : $"<a href=\"{WebUtility.HtmlEncode(r.PostLink)}\">Link</a>";

                    sb.Append("<tr>");
                    sb.Append(Td(r.PostTime.HasValue ? r.PostTime.Value.ToString("dd/MM/yyyy") : null));
                    sb.Append(Td(ChannelLabels.TryGetValue(r.ChannelType, out var channelLabel) ? channelLabel : null));
                    sb.Append(Td(link));
                    sb.Append(Td(r.View?.ToString("N0", CultureInfo.InvariantCulture)));
                    sb.Append(Td(r.Comment?.ToString("N0", CultureInfo.InvariantCulture)));
                    sb.Append(Td(r.Like?.ToString("N0", CultureInfo.InvariantCulture)));
                    sb.Append(Td(r.Save?.ToString("N0", CultureInfo.InvariantCulture)));
                    sb.Append(Td(r.Share?.ToString("N0", CultureInfo.InvariantCulture)));
                    sb.Append("</tr>");
                }

                sb.Append("</tbody></table></td></tr>");
            }
        }

        sb.Append("</tbody></table></body></html>");
        return sb.ToString();
    }

    private static string GetEnumDescription(Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attr = field?.GetCustomAttribute<DescriptionAttribute>();
        return attr?.Description ?? value.ToString();
    }
}
