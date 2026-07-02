using Abp.Application.Services;
using Abp.Net.Mail.Smtp;
using MqSocial.Emails.Dto;
using System;
using System.IO;
using System.Net.Mail;
using System.Threading.Tasks;

namespace MqSocial.Emails;

public class EmailAppService : ApplicationService, IEmailAppService
{
    private readonly ISmtpEmailSender _smtpEmailSender;

    public EmailAppService(ISmtpEmailSender smtpEmailSender)
    {
        _smtpEmailSender = smtpEmailSender;
    }

    public async Task SendAsync(SendEmailDto input)
    {
        var mail = new MailMessage
        {
            Subject = input.Subject,
            Body = input.Body,
            IsBodyHtml = input.IsBodyHtml,
        };

        foreach (var to in input.To)
            mail.To.Add(to);

        foreach (var cc in input.Cc ?? new())
            mail.CC.Add(cc);

        foreach (var att in input.Attachments ?? new())
        {
            var bytes = Convert.FromBase64String(att.Content);
            var stream = new MemoryStream(bytes);
            mail.Attachments.Add(new Attachment(stream, att.FileName, att.ContentType));
        }

        await _smtpEmailSender.SendAsync(mail);
    }
}
