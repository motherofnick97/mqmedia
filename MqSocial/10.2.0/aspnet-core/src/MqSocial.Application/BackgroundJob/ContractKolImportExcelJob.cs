using Abp.BackgroundJobs;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Microsoft.EntityFrameworkCore;
using MqSocial.BackgroundJob.Dto;
using MqSocial.Careers;
using MqSocial.Common.Enum;
using MqSocial.ContractKols;
using MqSocial.Contracts;
using MqSocial.KolCareers;
using MqSocial.KolDuplicateContracts;
using MqSocial.Kols;
using MqSocial.Kols.Dto;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MqSocial.BackgroundJob
{
    public class ContractKolImportExcelJob : AsyncBackgroundJob<ContractKolImportExcelJobArgs>, ITransientDependency
    {
        private readonly IRepository<Kol, Guid> _kolRepository;
        private readonly IRepository<KolCarrer, Guid> _kolCareerRepository;
        private readonly IRepository<ContractKol, Guid> _contractKolRepository;
        private readonly IRepository<KolDuplicateContract, Guid> _kolDuplicateContractRepository;
        private readonly IRepository<Contract, Guid> _contractRepository;

        public ContractKolImportExcelJob(IRepository<Kol, Guid> kolRepository,
            IRepository<KolCarrer, Guid> kolCareerRepository,
            IRepository<ContractKol, Guid> contractKolRepository,
            IRepository<KolDuplicateContract, Guid> kolDuplicateContractRepository,
            IRepository<Contract, Guid> contractRepository
            )
        {
            _kolRepository = kolRepository;
            _kolCareerRepository = kolCareerRepository;
            _contractKolRepository = contractKolRepository;
            _kolDuplicateContractRepository = kolDuplicateContractRepository;
            _contractRepository = contractRepository;
        }

        [UnitOfWork]
        public override async Task ExecuteAsync(ContractKolImportExcelJobArgs args)
        {
            try
            {
                using (CurrentUnitOfWork.SetTenantId(args.TenantId))
                {
                    ChannelType channel = ChannelType.Khac;
                    if (!string.IsNullOrEmpty(args.AhannelText) && Enum.TryParse<ChannelType>(args.AhannelText, true, out var ch))
                        channel = ch;

                    var effectiveCareerIds = new List<Guid>();
                    if (args.CareerIds != null && args.CareerIds.Count > 0)
                    {
                        effectiveCareerIds = args.CareerIds;
                    }

                    var dupKolContractNames = await GetKolDuplicateContractNamesAsync(args.ContractId);

                    if (args.ContractId.HasValue && await CheckContractKolAdded(args.AccountId, channel, args.ContractId.Value))
                    {
                        Logger.Info("Skip: KOL already added to contract. AccountId= " + args.AccountId);
                        return;
                    }

                    if (args.ContractId.HasValue && dupKolContractNames != null)
                    {
                        var conflictingContractNames = await GetKolConflictingContractNames(args.AccountId, channel, dupKolContractNames);
                        if (conflictingContractNames != null)
                        {
                            Logger.Info($"Skip: KOL đã nằm trong hợp đồng không được phép trùng: {conflictingContractNames}. AccountId= {args.AccountId}");
                            return;
                        }
                    }

                    KolDto kolDto = await CrawlUserInfo(args.AccountId);

                    if (kolDto == null)
                    {
                        Logger.Error("Fail when crawl data: AccountId= " + args.AccountId);
                        //result.Errors.Add(new ImportKolErrorDto { Row = row, Message = "Fail when crawl data" });
                        //result.FailCount++;
                        //continue;
                        return;
                    }

                    Guid kolId = await HandleKolFromExcel(kolDto, args.AccountId, channel, args.Address, args.Note, args.Phone, args.Age, args.OtherContact, effectiveCareerIds, args.TenantId);

                    if (args.ContractId.HasValue)
                    {
                        await HanleContractKolFromExcel(kolId, args.ContractId.Value, args.TenantId);
                    }

                    Logger.Info("Success when crawl data: AccountId= " + args.AccountId);
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Fail when crawl data: AccountId= " + args.AccountId + " - " + ex.Message);
                return;
            }
        }

        private async Task<KolDto> CrawlUserInfo(string uniqueId)
        {
            const int maxAttempts = 3;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
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
                catch (Exception ex)
                {
                    Logger.Error($"Fail when crawl data (attempt {attempt}/{maxAttempts}): AccountId= {uniqueId} - {ex.Message}");
                }

                if (attempt < maxAttempts)
                    await Task.Delay(1000);
            }

            return null;
        }

        private async Task<Guid> HandleKolFromExcel(KolDto kolDto, string accountId, ChannelType channel, string address, string note, string phone, string age, string otherContact, List<Guid> effectiveCareerIds, int? tenantId)
        {
            Guid kolId = new Guid();

            var exists = await _kolRepository.GetAll()
                    .FirstOrDefaultAsync(x => x.AccountId == accountId && x.Channel == channel);
            if (exists != null)
            {
                kolId = exists.Id;

                exists.Follow = kolDto.Follow;
                exists.Name = kolDto.Name;
                exists.Address = address;
                exists.Note = note;
                exists.Phone = phone;
                exists.OtherContacts = otherContact;
                exists.Age = string.IsNullOrEmpty(age) ? null : Int32.Parse(age);
                exists.Link = channel == ChannelType.Tiktok ? $"https://www.tiktok.com/@{exists.AccountId}"
                                : (channel == ChannelType.Facebook ? $"https://www.facebook.com/{exists.AccountId}/"
                                    : string.Empty);

                foreach (var careerId in effectiveCareerIds)
                {
                    var alreadyLinked = await _kolCareerRepository.GetAll()
                        .AnyAsync(x => x.KolId == exists.Id && x.CareerId == careerId);
                    if (!alreadyLinked)
                        await _kolCareerRepository.InsertAsync(new KolCarrer { KolId = exists.Id, CareerId = careerId, TenantId = tenantId });
                }
            }
            else
            {
                var newKol = await _kolRepository.InsertAsync(new Kol
                {
                    Name = kolDto.Name,
                    AccountId = accountId,
                    Channel = channel,
                    TenantId = tenantId,
                    Follow = kolDto.Follow,
                    Note = note,
                    Address = address,
                    Phone = phone,
                    OtherContacts = otherContact,
                    Age = string.IsNullOrEmpty(age) ? null : Int32.Parse(age),
                    Link = channel == ChannelType.Tiktok ? $"https://www.tiktok.com/@{accountId}"
                                        : (channel == ChannelType.Facebook ? $"https://www.facebook.com/{accountId}/"
                                            : string.Empty)
                });

                kolId = newKol.Id;

                foreach (var careerId in effectiveCareerIds)
                    await _kolCareerRepository.InsertAsync(new KolCarrer { KolId = newKol.Id, CareerId = careerId, TenantId = tenantId });
            }
            return kolId;


        }

        private async Task HanleContractKolFromExcel(Guid kolId, Guid contractId, int? tenantId)
        {

            var contractKolExists = await _contractKolRepository.GetAll()
                           .AnyAsync(x => x.KolId == kolId && x.ContractId == contractId);
            if (!contractKolExists)
                await _contractKolRepository.InsertAsync(new ContractKol
                {
                    KolId = kolId,
                    ContractId = contractId,
                    Status = ContractKolStatus.Register,
                    TenantId = tenantId,
                    SampleReceiveStatus = ReceiveStatus.NotShip
                });
        }

        // Kiểm tra KOL (theo AccountId/uniqueId + Channel) đã có trong hợp đồng chưa — check trước khi
        // gọi CrawlUserInfo để không tốn quota API crawl cho những dòng Excel đã import trước đó.
        private async Task<bool> CheckContractKolAdded(string uniqueId, ChannelType channel, Guid contractId)
        {
            return await _contractKolRepository.GetAll()
                .Join(_kolRepository.GetAll(),
                    ck => ck.KolId,
                    k => k.Id,
                    (ck, k) => new { ck.ContractId, k.AccountId, k.Channel })
                .AnyAsync(x => x.ContractId == contractId && x.AccountId == uniqueId && x.Channel == channel);
        }

        // KOL đã tồn tại từ trước (crawl lần này chỉ để cập nhật) mới có thể đã nằm trong hợp đồng khác
        // — check bằng KolId sẵn có, không cần crawl mới xác định được. Trả về danh sách tên hợp đồng
        // xung đột (nối chuỗi bằng ", "), hoặc null nếu KOL chưa tồn tại/không xung đột.
        private async Task<string> GetKolConflictingContractNames(string accountId, ChannelType channel, Dictionary<Guid, List<string>> dupKolContractNames)
        {
            var existingKol = await _kolRepository.GetAll()
                .FirstOrDefaultAsync(x => x.AccountId == accountId && x.Channel == channel);

            if (existingKol != null && dupKolContractNames.TryGetValue(existingKol.Id, out var conflictingContracts))
                return string.Join(", ", conflictingContracts);

            return null;
        }

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

    }
}
