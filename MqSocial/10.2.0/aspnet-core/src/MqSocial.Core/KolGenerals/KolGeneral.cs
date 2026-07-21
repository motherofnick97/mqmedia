using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using MqSocial.Common.Enum;
using System;
using System.ComponentModel.DataAnnotations;

namespace MqSocial.KolGenerals
{
    public class KolGeneral : FullAuditedEntity<Guid>, IMayHaveTenant
    {
        public int? TenantId { get; set; }

        [Required]
        public string FullName { get; set; }

        public string Phone { get; set; }

        public string Address { get; set; }

        public DateTime Dob { get; set; }

        public string Identity { get; set; }

        public Bank Bank { get; set; }

        public string BankNumber { get; set; }

        public string BankOwner { get; set; }
    }
}
