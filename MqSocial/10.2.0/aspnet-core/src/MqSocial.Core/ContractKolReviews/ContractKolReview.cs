using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using MqSocial.ContractKols;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MqSocial.ContractKolReviews
{
    public class ContractKolReview : FullAuditedEntity<Guid>, IMayHaveTenant
    {
        public int? TenantId { get; set; }

        public string Review { get; set; }

        public Guid ContractKolId { get; set; }

        [ForeignKey("ContractKolId")]
        public ContractKol ContractKol { get; set; }
    }
}
