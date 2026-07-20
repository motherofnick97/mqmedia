using Abp.Authorization;
using Microsoft.AspNetCore.Mvc;
using MqSocial.Authorization;
using MqSocial.Controllers;
using System;
using System.IO;

namespace MqSocial.Web.Host.Controllers
{
    // Route đặt dưới "api/" để nginx (chỉ proxy /api, /signalr, và vài route ABP cố định) chuyển đúng về backend
    // mà không cần sửa thêm cấu hình nginx trên server.
    [Route("api/ContractTemplateFiles")]
    [AbpAuthorize(PermissionNames.Pages_ContractTemplates)]
    public class ContractTemplateFilesController : MqSocialControllerBase
    {
        [HttpGet("{fileName}")]
        public IActionResult Download(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName)
            {
                return BadRequest();
            }

            var storageRoot = Path.Combine(AppContext.BaseDirectory, "ContractTemplates", "Templates");
            var fullPath = Path.Combine(storageRoot, fileName);

            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound();
            }

            return PhysicalFile(fullPath, "application/octet-stream", fileName);
        }
    }
}
