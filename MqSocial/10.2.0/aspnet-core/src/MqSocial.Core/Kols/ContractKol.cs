using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using MqSocial.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MqSocial.Kols
{
    public class ContractKol : FullAuditedEntity<int>, IMayHaveTenant
    {
        public int? TenantId { get; set; }

        public int KolId { get; set; }

        [ForeignKey("KolId")]
        public Kol Kol { get; set; }

        public string ContractId { get; set; }

        [ForeignKey("ContractId")]
        public Contract Contract { get; set; }

        public ContractKolStatus Status { get; set; }

        public int Cash { get; set; }

        public int Payment { get; set; }
    }
}
