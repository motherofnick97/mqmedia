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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml;

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

    public async Task<CreateKolDto> CrawlKolInfoByUrl(CrawlKolInfoInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Url))
            throw new UserFriendlyException("URL không hợp lệ");

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

        string html = await httpClient.GetStringAsync(input.Url);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // 1. Lấy nội dung JSON trong thẻ script
        var scriptNode = doc.GetElementbyId("__UNIVERSAL_DATA_FOR_REHYDRATION__");
        if (scriptNode == null)
            throw new Exception("Không tìm thấy script data trên trang");

        string json = scriptNode.InnerText;

        // 2. Parse JSON và lấy followerCount
        var root = JObject.Parse(json);
        var userNode = root["__DEFAULT_SCOPE__"]?
                ["webapp.user-detail"]?
                ["userInfo"]?
                ["user"];

        string uniqueId = userNode?["uniqueId"]?.Value<string>() ?? "";

        string name = userNode?["nickname"]?.Value<string>() ?? "";

        var statsNode = root["__DEFAULT_SCOPE__"]?
                        ["webapp.user-detail"]?
                        ["userInfo"]?
                        ["stats"];

        var followerCount = statsNode?["followerCount"]?.Value<int>() ?? 0;

        CreateKolDto kolDto = new CreateKolDto()
        {
            Follow = followerCount,
            AccountId = uniqueId,
            Name = name,
            Link = input.Url,
            Channel = input.Channel
        };

        var existingKol = await Repository.GetAll()
        .FirstOrDefaultAsync(x => x.AccountId == uniqueId && x.Channel == input.Channel);

        if (existingKol != null)
        {
            existingKol.Follow = followerCount;
            await Repository.UpdateAsync(existingKol);
        }
        else
        {
            await Repository.InsertAsync(ObjectMapper.Map<Kol>(kolDto));
        }

        return kolDto;
    }

}
