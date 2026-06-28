using Abp.Application.Services;
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
using MqSocial.KolCareers;
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
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MqSocial.Kols;

//[AbpAuthorize(PermissionNames.Pages_Kols)]
public class KolAppService : AsyncCrudAppService<Kol, KolDto, Guid, PagedKolRequestDto, CreateKolDto, KolDto>, IKolAppService
{
    public ILogger Logger { get; set; }

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IRepository<Career, Guid> _careerRepository;
    private readonly IRepository<KolCarrer, Guid> _kolCareerRepository;
    private readonly IRepository<ContractKol, Guid> _contractKolRepository;

    public KolAppService(
        IRepository<Kol, Guid> repository,
        IHttpClientFactory httpClientFactory,
        IRepository<Career, Guid> careerRepository,
        IRepository<KolCarrer, Guid> kolCareerRepository,
        IRepository<ContractKol, Guid> contractKolRepository)
        : base(repository)
    {
        _httpClientFactory = httpClientFactory;
        _careerRepository = careerRepository;
        _kolCareerRepository = kolCareerRepository;
        _contractKolRepository = contractKolRepository;
    }

    protected override IQueryable<Kol> CreateFilteredQuery(PagedKolRequestDto input)
    {
        return Repository.GetAll()
            .Include(x => x.KolCareers).ThenInclude(x => x.Career)
            .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x =>
                x.Name.Contains(input.Keyword) ||
                x.Note.Contains(input.Keyword))
            .WhereIf(input.CareerId.HasValue, x => x.KolCareers.Any(k => k.CareerId == input.CareerId.Value))
            .WhereIf(input.Channel.HasValue, x => x.Channel == input.Channel.Value);
    }

    protected override IQueryable<Kol> ApplySorting(IQueryable<Kol> query, PagedKolRequestDto input)
    {
        return query.OrderBy(input.Sorting);
    }

    public override async Task<KolDto> CreateAsync(CreateKolDto input)
    {
        CheckCreatePermission();

        var kol = MapToEntity(input);
        await Repository.InsertAsync(kol);
        //await CurrentUnitOfWork.SaveChangesAsync();

        foreach (var careerId in input.CareerIds ?? new List<Guid>())
        {
            await _kolCareerRepository.InsertAsync(new KolCarrer { KolId = kol.Id, CareerId = careerId });
        }

        //await CurrentUnitOfWork.SaveChangesAsync();

        var reloaded = await Repository.GetAll()
            .Include(x => x.KolCareers).ThenInclude(x => x.Career)
            .FirstOrDefaultAsync(x => x.Id == kol.Id);

        return MapToEntityDto(reloaded);
    }

    public override async Task<KolDto> UpdateAsync(KolDto input)
    {
        CheckUpdatePermission();

        var kol = await Repository.GetAsync(input.Id);
        MapToEntity(input, kol);
        await Repository.UpdateAsync(kol);

        // Xóa KolCareer cũ, chèn mới
        var existing = await _kolCareerRepository.GetAll()
            .Where(x => x.KolId == kol.Id)
            .ToListAsync();
        foreach (var item in existing)
            await _kolCareerRepository.DeleteAsync(item);

        foreach (var careerId in input.CareerIds ?? new List<Guid>())
        {
            await _kolCareerRepository.InsertAsync(new KolCarrer { KolId = kol.Id, CareerId = careerId });
        }

        //await CurrentUnitOfWork.SaveChangesAsync();

        var reloaded = await Repository.GetAll()
            .Include(x => x.KolCareers).ThenInclude(x => x.Career)
            .FirstOrDefaultAsync(x => x.Id == kol.Id);

        return MapToEntityDto(reloaded);
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

        for (int row = 2; row <= rowCount; row++)
        {
            try
            {
                var accountId = sheet.Cells[row, 1].Text?.Trim();
                var channelText = sheet.Cells[row, 2].Text?.Trim();
                var careerText = sheet.Cells[row, 3].Text?.Trim();
                var follow = sheet.Cells[row, 4].Text?.Trim();
                var phone = sheet.Cells[row, 5].Text?.Trim();
                var address = sheet.Cells[row, 6].Text?.Trim();
                var note = sheet.Cells[row, 7].Text?.Trim();

                if (string.IsNullOrEmpty(accountId))
                {
                    result.Errors.Add(new ImportKolErrorDto { Row = row, Message = "Mã tài khoản không được để trống" });
                    result.FailCount++;
                    continue;
                }

                ChannelType? channel = null;
                if (!string.IsNullOrEmpty(channelText) && Enum.TryParse<ChannelType>(channelText, true, out var ch))
                    channel = ch;

                // Dùng CareerIds từ FE nếu có, không thì lookup từ cột Excel
                var effectiveCareerIds = new List<Guid>();
                if (input.CareerIds != null && input.CareerIds.Count > 0)
                {
                    effectiveCareerIds = input.CareerIds;
                }
                else if (!string.IsNullOrEmpty(careerText))
                {
                    var careerEntity = await _careerRepository.GetAll().FirstOrDefaultAsync(x => x.Name == careerText);
                    if (careerEntity != null) effectiveCareerIds.Add(careerEntity.Id);
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
                    await HanleContractKolFromExcel(kolId, input.ContractId.Value);
                    var contractKolExists = await _contractKolRepository.GetAll()
                        .AnyAsync(x => x.KolId == kolId && x.ContractId == input.ContractId.Value);
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

    private async Task<Guid> HandleKolFromExcel(KolDto kolDto, string accountId, ChannelType? channel, string address, string note, string phone, List<Guid> effectiveCareerIds)
    {
        Guid kolId = new Guid();

        var exists = await Repository.GetAll()
                    .FirstOrDefaultAsync(x => x.AccountId == accountId && x.Channel == channel.Value);
        if (exists != null)
        {
            kolId = exists.Id;

            exists.Follow = kolDto.Follow;
            exists.Name = kolDto.Name;
            exists.Address = address;
            exists.Note = note;
            exists.Phone = phone;

            foreach (var careerId in effectiveCareerIds)
            {
                var alreadyLinked = await _kolCareerRepository.GetAll()
                    .AnyAsync(x => x.KolId == exists.Id && x.CareerId == careerId);
                if (!alreadyLinked)
                    await _kolCareerRepository.InsertAsync(new KolCarrer { KolId = exists.Id, CareerId = careerId });
            }
        }
        else
        {
            var newKol = await Repository.InsertAsync(new Kol
            {
                Name = kolDto.Name,
                AccountId = accountId,
                Channel = channel ?? ChannelType.Khac,
                Follow = kolDto.Follow,
                Note = note,
                Address = address,
                Phone = phone,
            });

            foreach (var careerId in effectiveCareerIds)
                kolId = await _kolCareerRepository.InsertAndGetIdAsync(new KolCarrer { KolId = newKol.Id, CareerId = careerId });
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
                Status = ContractKolStatus.Register
            });
    }
}
