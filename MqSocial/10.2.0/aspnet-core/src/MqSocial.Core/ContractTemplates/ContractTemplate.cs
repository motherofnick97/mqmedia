using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using System;
using System.ComponentModel.DataAnnotations;

namespace MqSocial.ContractTemplates
{
    public class ContractTemplate : FullAuditedEntity<Guid>, IMayHaveTenant
    {
        [Required]
        public string Name { get; set; }

        public string FilePath { get; set; }

        public int? TenantId { get; set; }
    }
}
