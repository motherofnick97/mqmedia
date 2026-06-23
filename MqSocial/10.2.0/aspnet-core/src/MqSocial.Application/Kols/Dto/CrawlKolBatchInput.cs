using MqSocial.Common.Enum;
using System.Collections.Generic;

namespace MqSocial.Kols.Dto;

public class CrawlKolBatchInput
{
    /// <summary>
    /// Danh sách URL TikTok, ví dụ: ["https://www.tiktok.com/@trangxinh_1703", ...]
    /// </summary>
    public List<string> Urls { get; set; } = new();

    public ChannelType Channel { get; set; }
}