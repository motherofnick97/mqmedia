using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MqSocial.Emails.Dto;

public class SendEmailDto
{
    [Required]
    public List<string> To { get; set; } = new();

    public List<string> Cc { get; set; } = new();

    [Required]
    public string Subject { get; set; }

    [Required]
    public string Body { get; set; }

    public bool IsBodyHtml { get; set; } = true;

    public List<EmailAttachmentDto> Attachments { get; set; } = new();
}
