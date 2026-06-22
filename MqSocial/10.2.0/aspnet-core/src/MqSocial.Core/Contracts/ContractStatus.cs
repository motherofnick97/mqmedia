using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MqSocial.Contracts
{
    public enum ContractStatus
    {
        [Description("Chuẩn bị")]
        Prepare = 1,
        [Description("Đang triển khai")]
        Processing = 2,
        [Description("Hoàn thành")]
        Complete = 3,
        [Description("Tạm dừng")]
        Pause = 4,
        [Description("Hủy")]
        Cancel = 5
    }
}
