using Microsoft.AspNetCore.Http;

namespace MqSocial.ContractTemplates.Dto;

public class UploadContractTemplateFileDto
{
    public IFormFile File { get; set; }
}

public class UploadContractTemplateFileResultDto
{
    public string FilePath { get; set; }

    public string FileName { get; set; }
}
