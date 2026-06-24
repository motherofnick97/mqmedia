using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MqSocial.ContractKols
{
    public enum ContractKolStatus
    {
        [Description("Đăng ký")]
        Register = 1,
        [Description("Duyệt đăng ký")]
        Approve,
        [Description("Đang tiến hành")]
        Processing,
        [Description("Mkt duyệt")]
        MktOk,
        [Description("Quản lý duyệt")]
        DpmOk,
        [Description("Đã air")]
        OnAir,
        [Description("Theo dõi")]
        Following,
        [Description("Đã thanh toán")]
        Paid,
        [Description("Hoàn thành")]
        Done,
        [Description("Hủy")]
        Cancel,
        [Description("Từ chối")]
        Reject
    }
}
