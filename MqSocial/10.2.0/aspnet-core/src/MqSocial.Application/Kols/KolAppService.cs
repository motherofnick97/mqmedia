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
}