using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MqSocial.Companies
{
    public class Company : FullAuditedEntity<int>, IMayHaveTenant
    {
        [Required]
        public string Name { get; set; }

        public int? TenantId { get; set ; }
    }
}
