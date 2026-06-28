using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

public class UpdateContractKolResultJob
{
    private readonly ILogger<UpdateContractKolResultJob> _logger;
    private readonly DbConnectionFactory _db;

    public UpdateContractKolResultJob(ILogger<UpdateContractKolResultJob> logger, DbConnectionFactory db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task Execute()
    {
        _logger.LogInformation("[UpdateContractKolResult] Bắt đầu: {Time}", DateTime.Now);

        var records = await GetPendingRecordsAsync();
        _logger.LogInformation("[UpdateContractKolResult] Tìm thấy {Count} record cần update", records.Count);

        foreach (var record in records)
        {
            try
            {
                var metrics = await FetchMetricsAsync(record.ChannelType, record.PostLink);
                if (metrics is not null)
                    await UpdateMetricsAsync(record.Id, metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateContractKolResult] Lỗi khi xử lý record {Id}", record.Id);
            }
        }

        _logger.LogInformation("[UpdateContractKolResult] Hoàn tất");
    }

    private async Task<List<ContractKolResultRecord>> GetPendingRecordsAsync()
    {
        var results = new List<ContractKolResultRecord>();
        const string sql = """
            SELECT 
            	A.Id Id, PostLink, ChannelType
            FROM 
            	ContractKolResults A
            inner join 
            	ContractKols B
            on 
            	A.ContractKolId = B.Id
            inner join 
            	Contracts C
            on 
            	B.ContractId = C.Id
            WHERE 
            	A.IsDeleted = 0 and B.IsDeleted = 0 and C.IsDeleted = 0
            	AND PostLink IS NOT NULL
            	AND PostLink != ''
            	and C.Status = 2
            """;

        await using var conn = _db.Create();
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(new ContractKolResultRecord(
                Id: reader.GetGuid(0),
                PostLink: reader.GetString(1),
                ChannelType: reader.GetInt32(2)
            ));
        }

        return results;
    }

    private async Task UpdateMetricsAsync(Guid id, PostMetrics metrics)
    {
        const string sql = """
            UPDATE ContractKolResults
            SET [View]    = @View,
                [Like]  = @Like,
                [Comment] = @Comment,
                [Save]    = @Save,
                [Share]   = @Share,
                LastModificationTime = @Now
            WHERE Id = @Id
            """;

        await using var conn = _db.Create();
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@View", (object?)metrics.View ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Comment", (object?)metrics.Comment ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Save", (object?)metrics.Save ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Share", (object?)metrics.Share ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Like", (object?)metrics.Like ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<PostMetrics?> FetchMetricsAsync(int channelType, string postLink)
    {
        return channelType switch
        {
            1 => await FetchTikTokMetricsAsync(postLink),
            2 => await FetchFacebookMetricsAsync(postLink),
            _ => null
        };
    }

    private async Task<PostMetrics?> FetchTikTokMetricsAsync(string postLink)
    {
        string videoId = string.Empty;
        var match = Regex.Match(postLink, @"/video/(\d+)");
        if (match.Success)
        {
            videoId = match.Groups[1].Value;
        }

        if (string.IsNullOrEmpty(videoId)) return null;

        var client = new HttpClient();
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("https://tiktok-api23.p.rapidapi.com/api/post/detail?videoId=" + videoId),
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

        int SafeInt(JToken token)
        {
            if (token.Type == JTokenType.Integer) return token.Value<int>();
            if (token.Type == JTokenType.String && int.TryParse(token.Value<string>(), out var val)) return val;
            return 0;
        }
        var stats = json["itemInfo"]["itemStruct"]["stats"];
        var diggCount = SafeInt(stats["diggCount"]);
        var shareCount = SafeInt(stats["shareCount"]);
        var commentCount = SafeInt(stats["commentCount"]);
        var playCount = SafeInt(stats["playCount"]);
        var collectCount = SafeInt(stats["collectCount"]);
        // TODO: Gọi TikTok API hoặc crawl để lấy View, Comment, Share
        await Task.CompletedTask;
        return new PostMetrics(playCount, commentCount, collectCount, shareCount, diggCount);
    }

    private async Task<PostMetrics?> FetchFacebookMetricsAsync(string postLink)
    {
        // TODO: Gọi Facebook Graph API để lấy View, Comment, Share
        await Task.CompletedTask;
        return null;
    }

    private record ContractKolResultRecord(Guid Id, string PostLink, int ChannelType);

    private record PostMetrics(int? View, int? Comment, int? Save, int? Share, int? Like);

    
}
