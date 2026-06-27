using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using MqSocial.Common.Enum;
using MqSocial.ContractKols;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MqSocial.ContractKolResults
{
    public class ContractKolResult : FullAuditedEntity<Guid>, IMayHaveTenant
    {
        public int? TenantId { get; set; }

        public Guid ContractKolId { get; set; }
        
        [ForeignKey("ContractKolId")]
        public ContractKol ContractKol { get; set; }

        public DateTime? PostTime { get; set; }

        public string PostLink { get; set; }

        public int? View { get; set; }

        public int? Comment { get; set; }

        public int? Save { get; set; }

        public int? Share { get; set; }

        public int? Like { get; set; }

        public ChannelType ChannelType { get; set; }
    }
}
