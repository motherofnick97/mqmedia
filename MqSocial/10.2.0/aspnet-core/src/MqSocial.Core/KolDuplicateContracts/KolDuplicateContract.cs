using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using MqSocial.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MqSocial.KolDuplicateContracts
{
    public class KolDuplicateContract : FullAuditedEntity<Guid>, IMayHaveTenant
    {
        public int? TenantId { get; set; }

        public Guid FirstContractId { get; set; }

        public Guid SecondContractId { get; set; }

    }
}
