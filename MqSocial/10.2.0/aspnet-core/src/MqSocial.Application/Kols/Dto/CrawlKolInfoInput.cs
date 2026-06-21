using MqSocial.Common.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MqSocial.Kols.Dto
{
    public class CrawlKolInfoInput
    {
        [Required]
        public string Url { get; set; }

        [Required]
        public ChannelType Channel { get; set; }
    }
}
