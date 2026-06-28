using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using MqSocial.Careers;
using MqSocial.Kols;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MqSocial.KolCareers
{
    public class KolCarrer : FullAuditedEntity<Guid>, IMayHaveTenant
    {
        public int? TenantId { get; set; }

        public Guid CareerId { get; set; }

        [ForeignKey("CareerId")]
        public Career Career { get; set; }

        public Guid KolId { get; set; }

        [ForeignKey("KolId")]
        public Kol Kol { get; set; }
    }
}
