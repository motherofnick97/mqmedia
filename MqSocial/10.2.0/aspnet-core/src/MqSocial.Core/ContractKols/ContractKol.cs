using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using MqSocial.Contracts;
using MqSocial.Kols;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MqSocial.ContractKols
{
    public class ContractKol : FullAuditedEntity<Guid>, IMayHaveTenant
    {
        public int? TenantId { get; set; }

        public Guid KolId { get; set; }

        [ForeignKey("KolId")]
        public Kol Kol { get; set; }

        public Guid ContractId { get; set; }

        [ForeignKey("ContractId")]
        public Contract Contract { get; set; }

        public ContractKolStatus Status { get; set; }

        public int Cash { get; set; }

        public int Payment { get; set; }

        public string Portrait { get; set; }

        public string ReviewCorner { get; set; }

        public string SampleSize { get; set; }

        public string SampleName { get; set; }

        public int SampleQuantity { get; set; }

        public ReceiveStatus SampleReceiveStatus { get; set; }

        public DateTime? AirTime { get; set; }

        public string Brief { get; set; }

        public string BriefLink { get; set; }

        public string Feedback { get; set; }

        public string Caption { get; set; }

        public string HashTag { get; set; }

        public string ReviewResult { get; set; }

    }
}
