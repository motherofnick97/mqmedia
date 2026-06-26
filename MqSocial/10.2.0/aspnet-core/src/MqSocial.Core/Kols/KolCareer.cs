using System.ComponentModel;

namespace MqSocial.Kols;

public enum KolCareer
{
    [Description("Khác")]
    Other = 0,
    [Description("Dược sĩ")]
    DuocSi = 1,
    [Description("Bác sĩ")]
    BacSi = 2,
    [Description("Mẹ bé")]
    Mom = 3
}

