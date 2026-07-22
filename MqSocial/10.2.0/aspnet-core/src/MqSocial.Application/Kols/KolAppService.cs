using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using Castle.Core.Logging;
using Castle.MicroKernel.Registration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MqSocial.Authorization;
using MqSocial.Careers;
using MqSocial.Common.Enum;
using MqSocial.CommonFunc;
using MqSocial.ContractKols;
using MqSocial.Contracts;
using MqSocial.KolCareers;
using MqSocial.KolDuplicateContracts;
using MqSocial.Kols.Dto;
using MqSocial.Roles.Dto;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MqSocial.Kols;

[AbpAuthorize(PermissionNames.Pages_Kols)]
public class KolAppService : AsyncCrudAppService<Kol, KolDto, Guid, PagedKolRequestDto, CreateKolDto, KolDto>, IKolAppService
{
    public ILogger Logger { get; set; }

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IRepository<Career, Guid> _careerRepository;
    private readonly IRepository<KolCarrer, Guid> _kolCareerRepository;
    private readonly IRepository<ContractKol, Guid> _contractKolRepository;
    private readonly IRepository<KolDuplicateContract, Guid> _kolDuplicateContractRepository;
    private readonly IRepository<Contract, Guid> _contractRepository;

    public KolAppService(
        IRepository<Kol, Guid> repository,
        IHttpClientFactory httpClientFactory,
        IRepository<Career, Guid> careerRepository,
        IRepository<KolCarrer, Guid> kolCareerRepository,
        IRepository<ContractKol, Guid> contractKolRepository,
        IRepository<KolDuplicateContract, Guid> kolDuplicateContractRepository,
        IRepository<Contract, Guid> contractRepository)
        : base(repository)
    {
        _httpClientFactory = httpClientFactory;
        _careerRepository = careerRepository;
        _kolCareerRepository = kolCareerRepository;
        _contractKolRepository = contractKolRepository;
        _kolDuplicateContractRepository = kolDuplicateContractRepository;
        _contractRepository = contractRepository;
    }

    // Trả về: KolId → danh sách tên contract bị duplicate với contractId đầu vào
    private async Task<Dictionary<Guid, List<string>>> GetKolDuplicateContractNamesAsync(Guid? contractId)
    {
        if (contractId == null)
            return new Dictionary<Guid, List<string>>();

        var duplicateContractIds = await _kolDuplicateContractRepository.GetAll()
            .Where(x => x.FirstContractId == contractId || x.SecondContractId == contractId)
            .Select(x => x.FirstContractId == contractId ? x.SecondContractId : x.FirstContractId)
            .ToListAsync();

        if (!duplicateContractIds.Any())
            return new Dictionary<Guid, List<string>>();

        var contractNames = await _contractRepository.GetAll()
            .Where(x => duplicateContractIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name);

        var contractKols = await _contractKolRepository.GetAll()
            .Where(x => duplicateContractIds.Contains(x.ContractId))
            .Select(x => new { x.KolId, x.ContractId })
            .ToListAsync();

        var result = new Dictionary<Guid, List<string>>();
        foreach (var ck in contractKols)
        {
            if (!result.ContainsKey(ck.KolId))
                result[ck.KolId] = new List<string>();

            var name = contractNames.TryGetValue(ck.ContractId, out var n) ? n : ck.ContractId.ToString();
            if (!result[ck.KolId].Contains(name))
                result[ck.KolId].Add(name);
        }

        return result;
    }

    protected override IQueryable<Kol> CreateFilteredQuery(PagedKolRequestDto input)
    {
        // Không ThenInclude(x => x.Career) ở đây: Career là danh mục dùng chung giữa các tenant
        // (TenantId luôn null, xem CareerAppService dùng CurrentUnitOfWork.SetTenantId(null)). Vì
        // CareerId là khóa ngoại bắt buộc trên KolCarrer, EF Core sẽ áp global tenant-filter của Career
        // lên cả navigation Include và loại luôn dòng KolCarrer nếu Career không khớp tenant hiện tại —
        // làm mất hết CareerIds/CareerNames. Tên career được nạp riêng (SetTenantId(null)) bên dưới.
        return Repository.GetAll()
            .Include(x => x.KolCareers)
            .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x =>
                x.Name.Contains(input.Keyword) ||
                x.Note.Contains(input.Keyword))
            .WhereIf(input.CareerId.HasValue, x => x.KolCareers.Any(k => k.CareerId == input.CareerId.Value))
            .WhereIf(input.Channel.HasValue, x => x.Channel == input.Channel.Value);
    }

    private async Task<Dictionary<Guid, string>> GetCareerNamesAsync(IEnumerable<Guid> careerIds)
    {
        var ids = careerIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        using (CurrentUnitOfWork.SetTenantId(null))
        {
            return await _careerRepository.GetAll()
                .Where(x => ids.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name);
        }
    }

    private async Task ApplyCareerNamesAsync(KolDto dto)
    {
        var careerNames = await GetCareerNamesAsync(dto.CareerIds);
        dto.CareerNames = dto.CareerIds
            .Select(id => careerNames.TryGetValue(id, out var name) ? name : null)
            .Where(name => name != null)
            .ToList();
    }

    public override async Task<PagedResultDto<KolDto>> GetAllAsync(PagedKolRequestDto input)
    {
        var result = await base.GetAllAsync(input);

        var careerNames = await GetCareerNamesAsync(result.Items.SelectMany(x => x.CareerIds));
        foreach (var item in result.Items)
        {
            item.CareerNames = item.CareerIds
                .Select(id => careerNames.TryGetValue(id, out var name) ? name : null)
                .Where(name => name != null)
                .ToList();
        }

        return result;
    }

    protected override IQueryable<Kol> ApplySorting(IQueryable<Kol> query, PagedKolRequestDto input)
    {
        return query.OrderBy(input.Sorting);
    }

    protected override Kol MapToEntity(CreateKolDto createInput)
    {
        var entity = base.MapToEntity(createInput);
        entity.TenantId = AbpSession.TenantId;
        return entity;
    }

    public override async Task<KolDto> GetAsync(EntityDto<Guid> input)
    {
        CheckGetPermission();

        var kol = await Repository.GetAll()
            .Include(x => x.KolCareers)
            .FirstOrDefaultAsync(x => x.Id == input.Id);

        var dto = MapToEntityDto(kol);
        await ApplyCareerNamesAsync(dto);
        return dto;
    }

    public override async Task<KolDto> CreateAsync(CreateKolDto input)
    {
        CheckCreatePermission();

        if (!string.IsNullOrWhiteSpace(input.AccountId))
        {
            var existing = await Repository.GetAll()
                .FirstOrDefaultAsync(x => x.AccountId == input.AccountId && x.Channel == input.Channel);

            if (existing != null)
                throw new UserFriendlyException($"KOL với AccountId '{input.AccountId}' trên kênh '{input.Channel}' đã tồn tại");
        }

        var kol = MapToEntity(input);
        await Repository.InsertAsync(kol);

        foreach (var careerId in input.CareerIds ?? new List<Guid>())
        {
            await _kolCareerRepository.InsertAsync(new KolCarrer { KolId = kol.Id, CareerId = careerId, TenantId = AbpSession.TenantId });
        }

        return MapToEntityDto(kol);
    }

    public override async Task<KolDto> UpdateAsync(KolDto input)
    {
        CheckUpdatePermission();

        var kol = await Repository.GetAsync(input.Id);
        MapToEntity(input, kol);
        await Repository.UpdateAsync(kol);

        var existing = await _kolCareerRepository.GetAll()
            .Where(x => x.KolId == kol.Id)
            .ToListAsync();
        foreach (var item in existing)
            await _kolCareerRepository.DeleteAsync(item);

        foreach (var careerId in input.CareerIds ?? new List<Guid>())
        {
            await _kolCareerRepository.InsertAsync(new KolCarrer { KolId = kol.Id, CareerId = careerId, TenantId = AbpSession.TenantId });
        }

        return MapToEntityDto(kol);
    }

    private async Task<KolDto> CrawlUserInfo(string uniqueId)
    {
        try
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://tiktok-api23.p.rapidapi.com/api/user/info?uniqueId=" + uniqueId),
                Headers =
            {
                { "x-rapidapi-key", "63fb251e9emsh10eab69a0126292p15c65cjsn4f2df599110c" },
                { "x-rapidapi-host", "tiktok-api23.p.rapidapi.com" },
            },
            };
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(body);

            var name = json["userInfo"]["user"]["nickname"].ToString();
            var followers = json["userInfo"]["stats"]["followerCount"].Value<int>();

            return new KolDto()
            {
                AccountId = uniqueId,
                Follow = followers,
                Name = name
            };
        }
        catch (Exception)
        {
            return null;
        }
        
    }

    public async Task<ImportKolResultDto> ImportFromExcel([FromForm] ImportKolDto input)
    {
        if (input.File == null || input.File.Length == 0)
            throw new UserFriendlyException("File không được để trống");

        var result = new ImportKolResultDto();

        using var stream = input.File.OpenReadStream();
        using var package = new ExcelPackage(stream);
        var sheet = package.Workbook.Worksheets[0];

        if (sheet == null)
            throw new UserFriendlyException("File Excel không có sheet nào");

        var rowCount = sheet.Dimension.Rows;

        var dupKolContractNames = await GetKolDuplicateContractNamesAsync(input.ContractId);

        for (int row = 2; row <= rowCount; row++)
        {
            try
            {
                var accountId = sheet.Cells[row, 1].Text?.Trim();
                var channelText = sheet.Cells[row, 2].Text?.Trim();
                var follow = sheet.Cells[row, 3].Text?.Trim();
                var phone = sheet.Cells[row, 4].Text?.Trim();
                var address = sheet.Cells[row, 5].Text?.Trim();
                var note = sheet.Cells[row, 6].Text?.Trim();

                if (string.IsNullOrEmpty(accountId))
                {
                    result.Errors.Add(new ImportKolErrorDto { Row = row, Message = "Mã tài khoản không được để trống" });
                    result.FailCount++;
                    continue;
                }

                ChannelType channel = ChannelType.Khac;
                if (!string.IsNullOrEmpty(channelText) && Enum.TryParse<ChannelType>(channelText, true, out var ch))
                    channel = ch;

                var effectiveCareerIds = new List<Guid>();
                if (input.CareerIds != null && input.CareerIds.Count > 0)
                {
                    effectiveCareerIds = input.CareerIds;
                }

                KolDto kolDto = await CrawlUserInfo(accountId);

                if (kolDto == null)
                {
                    result.Errors.Add(new ImportKolErrorDto { Row = row, Message = "Fail when crawl data" });
                    result.FailCount++;
                    continue;
                }

                Guid kolId = await HandleKolFromExcel(kolDto, accountId, channel, address, note, phone, effectiveCareerIds);

                if (input.ContractId.HasValue)
                {
                    if (dupKolContractNames.TryGetValue(kolId, out var conflictingContracts))
                    {
                        var names = string.Join(", ", conflictingContracts);
                        result.Duplicates.Add(new ImportKolErrorDto { Row = row, Message = $"KOL đã nằm trong hợp đồng: {names}" });
                        result.DuplicateCount++;
                        continue;
                    }
                    else
                    {
                        await HanleContractKolFromExcel(kolId, input.ContractId.Value);
                    }
                }

                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ImportKolErrorDto { Row = row, Message = ex.Message });
                result.FailCount++;
            }
        }

        return result;
    }

    private async Task<Guid> HandleKolFromExcel(KolDto kolDto, string accountId, ChannelType channel, string address, string note, string phone, List<Guid> effectiveCareerIds)
    {
        Guid kolId = new Guid();

        var exists = await Repository.GetAll()
                    .FirstOrDefaultAsync(x => x.AccountId == accountId && x.Channel == channel);
        if (exists != null)
        {
            kolId = exists.Id;

            exists.Follow = kolDto.Follow;
            exists.Name = kolDto.Name;
            exists.Address = address;
            exists.Note = note;
            exists.Phone = phone;
            exists.Link = channel == ChannelType.Tiktok ? $"https://www.tiktok.com/@{exists.AccountId}"
                            : (channel == ChannelType.Facebook ? $"https://www.facebook.com/{exists.AccountId}/" 
                                : string.Empty);

            foreach (var careerId in effectiveCareerIds)
            {
                var alreadyLinked = await _kolCareerRepository.GetAll()
                    .AnyAsync(x => x.KolId == exists.Id && x.CareerId == careerId);
                if (!alreadyLinked)
                    await _kolCareerRepository.InsertAsync(new KolCarrer { KolId = exists.Id, CareerId = careerId, TenantId = AbpSession.TenantId });
            }
        }
        else
        {
            var newKol = await Repository.InsertAsync(new Kol
            {
                Name = kolDto.Name,
                AccountId = accountId,
                Channel = channel,
                TenantId = AbpSession.TenantId,
                Follow = kolDto.Follow,
                Note = note,
                Address = address,
                Phone = phone,
                Link = channel == ChannelType.Tiktok ? $"https://www.tiktok.com/@{exists.AccountId}"
                                    : (channel == ChannelType.Facebook ? $"https://www.facebook.com/{exists.AccountId}/"
                                        : string.Empty)
            });

            kolId = newKol.Id;

            foreach (var careerId in effectiveCareerIds)
                await _kolCareerRepository.InsertAsync(new KolCarrer { KolId = newKol.Id, CareerId = careerId, TenantId = AbpSession.TenantId });
        }
        return kolId;
    }

    private async Task HanleContractKolFromExcel(Guid kolId, Guid contractId)
    {

        var contractKolExists = await _contractKolRepository.GetAll()
                       .AnyAsync(x => x.KolId == kolId && x.ContractId == contractId);
        if (!contractKolExists)
            await _contractKolRepository.InsertAsync(new ContractKol
            {
                KolId = kolId,
                ContractId = contractId,
                Status = ContractKolStatus.Register,
                TenantId = AbpSession.TenantId,
                SampleReceiveStatus = ReceiveStatus.NotShip
            });
    }
}
