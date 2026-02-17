using JeweleryAppBackend.Models;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace JeweleryAppBackend.Services;

public class EmailService
{
	private readonly EmailSettings _emailSettings;

	public EmailService(IOptions<EmailSettings> emailSettings)
	{
		_emailSettings = emailSettings.Value;
	}

    public async Task SendEmailAsync(
      string fromEmail,
      string toEmail,
      string subject,
      string emailBody,
      byte[]? pdfBytes = null,
      string pdfFileName = "attachment.pdf")
    {
        using var mailMessage = new MailMessage
        {
            From = new MailAddress(fromEmail, "The Carats HTX"),
            Subject = subject,
            Body = emailBody,
            IsBodyHtml = true,
        };

        mailMessage.To.Add(toEmail);

        if (pdfBytes != null && pdfBytes.Length > 0)
        {
            var stream = new MemoryStream(pdfBytes);
            stream.Position = 0;

            var attachment = new Attachment(stream, pdfFileName, "application/pdf");

            attachment.ContentDisposition.FileName = pdfFileName;
            attachment.ContentType.MediaType = "application/pdf";
            attachment.ContentDisposition.Inline = false;

            mailMessage.Attachments.Add(attachment);
        }

        using var client = new SmtpClient(_emailSettings.Smtp, _emailSettings.Port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(
                _emailSettings.Username,
                _emailSettings.Password),
            Timeout = 10000
        }; 
        try
        {
            await client.SendMailAsync(mailMessage);
        }
        catch (SmtpException ex)
        {
            // Better logging
            throw new Exception($"SMTP Error: {ex.StatusCode} - {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new Exception("Error sending email", ex);
        }
    }
}
