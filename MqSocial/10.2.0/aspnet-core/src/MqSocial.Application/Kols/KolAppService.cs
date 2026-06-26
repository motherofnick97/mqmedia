using Abp.Application.Services;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using Castle.Core.Logging;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MqSocial.Authorization;
using MqSocial.Authorization.Roles;
using MqSocial.Common.Enum;
using MqSocial.Kols.Dto;
using MqSocial.MultiTenancy;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Linq.Dynamic.Core;
using OfficeOpenXml;
using MqSocial.CommonFunc;

namespace MqSocial.Kols;

//[AbpAuthorize(PermissionNames.Pages_Kols)]
public class KolAppService : AsyncCrudAppService<Kol, KolDto, Guid, PagedKolRequestDto, CreateKolDto, KolDto>, IKolAppService
{
    public ILogger Logger { get; set; }

    private readonly IHttpClientFactory _httpClientFactory;

    public KolAppService(IRepository<Kol, Guid> repository, IHttpClientFactory httpClientFactory)
        : base(repository)
    {
        _httpClientFactory = httpClientFactory;
    }

    protected override IQueryable<Kol> CreateFilteredQuery(PagedKolRequestDto input)
    {
        return Repository.GetAll()
            .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x =>
                x.Name.Contains(input.Keyword) ||
                x.Note.Contains(input.Keyword))
            .WhereIf(input.Career.HasValue, x => x.Career == input.Career.Value)
            .WhereIf(input.Channel.HasValue, x => x.Channel == input.Channel.Value);
    }

    protected override IQueryable<Kol> ApplySorting(IQueryable<Kol> query, PagedKolRequestDto input)
    {
        return query.OrderBy(input.Sorting);
    }

    private async Task<KolDto> CrawlUserInfo(string uniqueId)
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
        var likes = json["userInfo"]["stats"]["heartCount"].Value<int>();

        return new KolDto()
        {
            AccountId = uniqueId,
            Follow = followers,
            Name = name
        };
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

        // Đọc từ dòng 2 (dòng 1 là header)
        for (int row = 2; row <= rowCount; row++)
        {
            try
            {//Tài khoản	Kênh	Nghề nghiệp	Follow	SĐT	Địa chỉ	Ghi chú
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

                // Parse channel
                ChannelType? channel = null;
                if (!string.IsNullOrEmpty(channelText) && Enum.TryParse<ChannelType>(channelText, true, out var ch))
                    channel = ch;

                // Parse Career
                KolCareer? career = CommonFunction.ParseEnumByDescription<KolCareer>(careerText);

                // Nếu muốn fallback thử parse theo tên enum luôn
                if (career == null && Enum.TryParse<KolCareer>(careerText, true, out var c))
                    career = c;

                KolDto kolDto = await CrawlUserInfo(accountId);

                // Check trung AccountId + Channel
                var exists = await Repository.GetAll().FirstOrDefaultAsync(x => x.AccountId == accountId && x.Channel == channel.Value);
                if (exists != null)
                {
                    exists.Follow = kolDto.Follow;
                    exists.Name = kolDto.Name;
                    exists.Address = address;
                    exists.Note = note;
                    exists.Phone = phone;
                    exists.Career = career ?? KolCareer.Other;
                    result.SuccessCount++;
                    continue;
                }

                await Repository.InsertAsync(new Kol
                {
                    Name = kolDto.Name,
                    AccountId = accountId,
                    Channel = channel ?? ChannelType.Khac,
                    Follow = kolDto.Follow,
                    Note = note,
                    Address = address,
                    Phone = phone,
                    Career = career ?? KolCareer.Other
                });

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

}