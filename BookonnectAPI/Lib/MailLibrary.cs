using BookonnectAPI.Configuration;
using BookonnectAPI.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace BookonnectAPI.Lib;

public class MailLibrary: IMailLibrary
{
	private MailSettingsOptions _mailSettings;
	private ILogger<MailLibrary> _logger;

	public MailLibrary(IOptionsSnapshot<MailSettingsOptions> mailSettings, ILogger<MailLibrary> logger)
	{
		_mailSettings = mailSettings.Value;
		_logger = logger;
    }

	public async Task SendMail(Email email)
	{
		try
		{

			MimeMessage emailMessage = new MimeMessage();
			MailboxAddress emailFrom = new MailboxAddress(_mailSettings.Name, _mailSettings.EmailId);
			emailMessage.From.Add(emailFrom);

			MailboxAddress emailTo = new MailboxAddress(email.Name, email.ToId);
			emailMessage.To.Add(emailTo);

			emailMessage.Subject = email.Subject;

			BodyBuilder bodyBuilder = new BodyBuilder();
			bodyBuilder.HtmlBody = email.Body;
			emailMessage.Body = bodyBuilder.ToMessageBody();

			using SmtpClient smtpClient = new SmtpClient();
            await smtpClient.ConnectAsync(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.None);
            await smtpClient.AuthenticateAsync(_mailSettings.UserName, _mailSettings.Password);
            await smtpClient.SendAsync(emailMessage);
            await smtpClient.DisconnectAsync(true);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex.Message, "Error sending email");
			throw;
		}
	}
}

