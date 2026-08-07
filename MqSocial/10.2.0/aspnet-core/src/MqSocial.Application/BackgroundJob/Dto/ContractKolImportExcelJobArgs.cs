using MqSocial.Common.Enum;
using MqSocial.Kols.Dto;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MqSocial.BackgroundJob.Dto
{
    [Serializable]
    public class ContractKolImportExcelJobArgs
    {

        public string AccountId { get; set; }
        public string AhannelText { get; set; }
        public string Follow { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Note { get; set; }
        public string Age { get; set; }
        public string OtherContact { get; set; }
        public List<Guid> CareerIds { get; set; }
        public int? TenantId { get; set; }
        public Guid? ContractId { get; set; }
    }
}
