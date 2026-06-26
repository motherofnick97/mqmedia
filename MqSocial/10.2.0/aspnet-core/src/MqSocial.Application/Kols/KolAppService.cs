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

    public async Task CrawlUserInfo(string uniqueId)
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

        var id =await Repository.InsertAndGetIdAsync(new Kol()
        {
            Name = name,
            AccountId = uniqueId,
            Channel = ChannelType.Tiktok,
            Follow = followers
        });

        return;
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
            {
                var name = sheet.Cells[row, 1].Text?.Trim();
                var accountId = sheet.Cells[row, 2].Text?.Trim();
                var channelText = sheet.Cells[row, 3].Text?.Trim();
                var followText = sheet.Cells[row, 4].Text?.Trim();
                var note = sheet.Cells[row, 5].Text?.Trim();

                if (string.IsNullOrEmpty(name))
                {
                    result.Errors.Add(new ImportKolErrorDto { Row = row, Message = "Tên không được để trống" });
                    result.FailCount++;
                    continue;
                }

                // Parse channel
                ChannelType? channel = null;
                if (!string.IsNullOrEmpty(channelText) && Enum.TryParse<ChannelType>(channelText, true, out var ch))
                    channel = ch;

                // Parse follow
                int.TryParse(followText, out var follow);

                // Check trung AccountId + Channel
                if (!string.IsNullOrEmpty(accountId) && channel.HasValue)
                {
                    var exists = await Repository.GetAll()
                        .AnyAsync(x => x.AccountId == accountId && x.Channel == channel.Value);
                    if (exists)
                    {
                        result.Errors.Add(new ImportKolErrorDto { Row = row, Message = $"KOL '{accountId}' đã tồn tại" });
                        result.FailCount++;
                        continue;
                    }
                }

                await CrawlUserInfo(accountId);

                //await Repository.InsertAsync(new Kol
                //{
                //    Name = name,
                //    AccountId = accountId,
                //    Channel = channel ?? ChannelType.Tiktok,
                //    Follow = follow,
                //    Note = note
                //});

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