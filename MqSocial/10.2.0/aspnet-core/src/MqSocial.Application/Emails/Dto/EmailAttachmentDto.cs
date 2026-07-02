using System.ComponentModel.DataAnnotations;

namespace MqSocial.Emails.Dto;

public class EmailAttachmentDto
{
    [Required]
    public string FileName { get; set; }

    [Required]
    public string Content { get; set; } // base64

    public string ContentType { get; set; } = "application/octet-stream";
}
