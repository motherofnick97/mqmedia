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

namespace MqSocial.Kols;

//[AbpAuthorize(PermissionNames.Pages_Kols)]
public class KolAppService : AsyncCrudAppService<Kol, KolDto, int, PagedKolRequestDto, CreateKolDto, KolDto>, IKolAppService
{
    public ILogger Logger { get; set; }

    private readonly IHttpClientFactory _httpClientFactory;

    public KolAppService(IRepository<Kol, int> repository, IHttpClientFactory httpClientFactory)
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
        return query.OrderBy(x => x.Name);
    }

    // Crawl 1 URL
    public async Task<CreateKolDto> CrawlKolInfoByUrl(CrawlKolInfoInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Url))
            throw new UserFriendlyException("URL không hợp lệ");

        var kolDto = await ParseTikTokProfile(input.Url, input.Channel);
        await UpsertKol(kolDto);
        return kolDto;
    }

    // Crawl batch nhiều URL
    public async Task<CrawlKolBatchResult> CrawlKolInfoByUrls(CrawlKolBatchInput input)
    {
        if (input.Urls == null || input.Urls.Count == 0)
            throw new UserFriendlyException("Danh sách URL không được rỗng");

        var result = new CrawlKolBatchResult();

        foreach (var url in input.Urls)
        {
            // Delay 2s giữa các request để tránh bị TikTok chặn
            await Task.Delay(TimeSpan.FromSeconds(2));

            try
            {
                var kolDto = await ParseTikTokProfile(url, input.Channel);
                await UpsertKol(kolDto);
                result.Success.Add(kolDto);
            }
            catch (Exception ex)
            {
                result.Failed.Add(new CrawlKolFailedItem
                {
                    Url = url,
                    Reason = ex.Message
                });
                Logger.Warn($"Crawl thất bại cho URL {url}: {ex.Message}");
            }
        }

        return result;
    }

    // ── Private helpers ───────────────────────────────────────────

    private async Task<CreateKolDto> ParseTikTokProfile(string url, ChannelType channel)
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
        httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        httpClient.DefaultRequestHeaders.Add("Referer", url);

        string html = await httpClient.GetStringAsync(url);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var scriptNode = doc.GetElementbyId("__UNIVERSAL_DATA_FOR_REHYDRATION__");
        if (scriptNode == null)
            throw new Exception($"Không tìm thấy script data — TikTok có thể đang chặn request cho: {url}");

        var root = JObject.Parse(scriptNode.InnerText);

        var userNode = root["__DEFAULT_SCOPE__"]?["webapp.user-detail"]?["userInfo"]?["user"];
        var statsNode = root["__DEFAULT_SCOPE__"]?["webapp.user-detail"]?["userInfo"]?["stats"];

        string uniqueId = userNode?["uniqueId"]?.Value<string>() ?? "";
        string name = userNode?["nickname"]?.Value<string>() ?? "";
        int followerCount = statsNode?["followerCount"]?.Value<int>() ?? 0;

        return new CreateKolDto
        {
            Follow = followerCount,
            AccountId = uniqueId,
            Name = name,
            Link = url,
            Channel = channel
        };
    }

    private async Task UpsertKol(CreateKolDto kolDto)
    {
        var existingKol = await Repository.GetAll()
            .FirstOrDefaultAsync(x => x.AccountId == kolDto.AccountId && x.Channel == kolDto.Channel);

        if (existingKol != null)
        {
            existingKol.Follow = kolDto.Follow;
            await Repository.UpdateAsync(existingKol);
        }
        else
        {
            await Repository.InsertAsync(ObjectMapper.Map<Kol>(kolDto));
        }
    }
}