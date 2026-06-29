using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using MqSocial.Campaigns;
using MqSocial.Common.Enum;
using MqSocial.Kols;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MqSocial.Contracts
{
    public class Contract : FullAuditedEntity<Guid>, IMayHaveTenant
    {
        public const int MaxNameLength = 256;
        public const int MaxDescriptionLength = 5000;

        [Required]
        [StringLength(MaxNameLength)]
        public string Name { get; set; }

        [StringLength(MaxDescriptionLength)]
        public string Note { get; set; }

        public Guid CampaignId { get; set; }

        [ForeignKey("CampaignId")]
        public Campaign Campaign { get; set; }

        public ContractStatus Status { get; set; }
        public int? TenantId { get; set; }

        public int? Budget { get; set; }

        public string Request { get; set; }

        public string Script { get; set; }

        public int MaxReviewTime { get; set; }
    }
}
