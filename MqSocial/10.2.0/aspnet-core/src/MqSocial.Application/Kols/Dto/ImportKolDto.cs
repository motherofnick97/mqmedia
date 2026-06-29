using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MqSocial.Kols.Dto
{
    public class ImportKolDto
    {
        public IFormFile File { get; set; }

        public List<Guid> CareerIds { get; set; } = new();

        public Guid? ContractId { get; set; }
    }

    public class ImportKolResultDto
    {
        public int SuccessCount { get; set; }

        public int DuplicateCount { get; set; }

        public int FailCount { get; set; }

        public List<ImportKolErrorDto> Errors { get; set; } = new();

        public List<ImportKolErrorDto> Duplicates { get; set; } = new();

    }

    public class ImportKolErrorDto
    {
        public int Row { get; set; }
        public string Message { get; set; }
    }

}
