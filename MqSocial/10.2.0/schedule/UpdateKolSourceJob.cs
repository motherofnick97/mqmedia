using Microsoft.Extensions.Logging;
using Npgsql;

public class UpdateKolSourceJob
{
    private readonly ILogger<UpdateKolSourceJob> _logger;
    private readonly DbConnectionFactory _db;

    public UpdateKolSourceJob(ILogger<UpdateKolSourceJob> logger, DbConnectionFactory db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task Execute()
    {
        _logger.LogInformation("[UpdateKolSource] Bắt đầu: {Time}", DateTime.Now);

        var kols = await GetTenantKolsMissingNullTenantCopyAsync();
        _logger.LogInformation("[UpdateKolSource] Tìm thấy {Count} KOL cần tạo bản ghi TenantId = null", kols.Count);

        foreach (var kol in kols)
        {
            try
            {
                await InsertNullTenantKolAsync(kol);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UpdateKolSource] Lỗi khi xử lý KOL {Id}", kol.Id);
            }
        }

        _logger.LogInformation("[UpdateKolSource] Hoàn tất");
    }

    // KOL "tương tự" được xác định theo cặp (AccountId, Channel) - khóa nghiệp vụ dùng để chống trùng KOL trong toàn hệ thống.
    // Nhiều tenant có thể có Kol trùng (AccountId, Channel) -> dùng DISTINCT ON để mỗi cặp chỉ lấy 1 dòng đại diện
    // (mới nhất theo CreationTime), tránh tạo nhiều bản ghi TenantId = null trùng nhau cho cùng 1 KOL.
    private async Task<List<KolRecord>> GetTenantKolsMissingNullTenantCopyAsync()
    {
        var results = new List<KolRecord>();
        const string sql = """
            SELECT DISTINCT ON (A."AccountId", A."Channel")
                A."Id", A."Name", A."Note", A."Link", A."Channel", A."GeneralCast",
                A."Follow", A."AccountId", A."Address", A."Phone"
            FROM
                "Kols" A
            WHERE
                A."IsDeleted" = false
                AND A."TenantId" IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1 FROM "Kols" B
                    WHERE B."IsDeleted" = false
                      AND B."TenantId" IS NULL
                      AND B."AccountId" = A."AccountId"
                      AND B."Channel" = A."Channel"
                )
            ORDER BY A."AccountId", A."Channel", A."CreationTime" DESC
            """;

        await using var conn = _db.Create();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(new KolRecord(
                Id: reader.GetGuid(0),
                Name: reader.GetString(1),
                Note: reader.IsDBNull(2) ? null : reader.GetString(2),
                Link: reader.IsDBNull(3) ? null : reader.GetString(3),
                Channel: reader.GetInt32(4),
                GeneralCast: reader.GetInt32(5),
                Follow: reader.GetInt32(6),
                AccountId: reader.GetString(7),
                Address: reader.IsDBNull(8) ? null : reader.GetString(8),
                Phone: reader.IsDBNull(9) ? null : reader.GetString(9)
            ));
        }

        return results;
    }

    private async Task InsertNullTenantKolAsync(KolRecord kol)
    {
        // Bọc thêm WHERE NOT EXISTS ngay trong INSERT để tự bảo vệ trước race-condition
        // (job chạy chồng lấn, hoặc có nhiều dòng cùng AccountId/Channel lọt qua bước SELECT).
        const string sql = """
            INSERT INTO "Kols"
                ("Id", "Name", "Note", "Link", "Channel", "GeneralCast", "Follow", "AccountId", "Address", "Phone", "TenantId", "IsDeleted", "CreationTime")
            SELECT @Id, @Name, @Note, @Link, @Channel, @GeneralCast, @Follow, @AccountId, @Address, @Phone, NULL, false, @Now
            WHERE NOT EXISTS (
                SELECT 1 FROM "Kols" B
                WHERE B."IsDeleted" = false
                  AND B."TenantId" IS NULL
                  AND B."AccountId" = @AccountId
                  AND B."Channel" = @Channel
            )
            """;

        await using var conn = _db.Create();
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("@Name", kol.Name);
        cmd.Parameters.AddWithValue("@Note", (object?)kol.Note ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Link", (object?)kol.Link ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Channel", kol.Channel);
        cmd.Parameters.AddWithValue("@GeneralCast", kol.GeneralCast);
        cmd.Parameters.AddWithValue("@Follow", kol.Follow);
        cmd.Parameters.AddWithValue("@AccountId", kol.AccountId);
        cmd.Parameters.AddWithValue("@Address", (object?)kol.Address ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Phone", (object?)kol.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow);
        await cmd.ExecuteNonQueryAsync();
    }

    private record KolRecord(
        Guid Id,
        string Name,
        string? Note,
        string? Link,
        int Channel,
        int GeneralCast,
        int Follow,
        string AccountId,
        string? Address,
        string? Phone);
}
