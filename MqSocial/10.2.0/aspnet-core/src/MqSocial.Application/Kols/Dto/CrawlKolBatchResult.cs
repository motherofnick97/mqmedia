using System.Collections.Generic;

namespace MqSocial.Kols.Dto;

public class CrawlKolBatchResult
{
    public List<CreateKolDto> Success { get; set; } = new();
    public List<CrawlKolFailedItem> Failed { get; set; } = new();

    public int TotalSuccess => Success.Count;
    public int TotalFailed => Failed.Count;
}

public class CrawlKolFailedItem
{
    public string Url { get; set; }
    public string Reason { get; set; }
}