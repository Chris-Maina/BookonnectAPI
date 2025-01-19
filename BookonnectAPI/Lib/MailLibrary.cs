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
	private IWebHostEnvironment _environment;

	public MailLibrary(IOptionsSnapshot<MailSettingsOptions> mailSettings, ILogger<MailLibrary> logger, IWebHostEnvironment environment)
	{
		_mailSettings = mailSettings.Value;
		_logger = logger;
        _environment = environment;
    }

	public async Task SendMail(Email email)
	{
		try
		{

			MimeMessage emailMessage = new MimeMessage();
			MailboxAddress emailFrom;
            if (email.FromName != null && email.FromId != null)
			{
				emailFrom = new MailboxAddress(email.FromName, email.FromId);
            }
			else
			{
				emailFrom = new MailboxAddress(_mailSettings.Name, _mailSettings.EmailId);
			}
			emailMessage.From.Add(emailFrom);

			MailboxAddress emailTo = new MailboxAddress(email.Name, email.ToId);
			emailMessage.To.Add(emailTo);

			emailMessage.Subject = email.Subject;

			BodyBuilder bodyBuilder = new BodyBuilder();
			bodyBuilder.HtmlBody = email.Body;
			emailMessage.Body = bodyBuilder.ToMessageBody();

			using SmtpClient smtpClient = new SmtpClient();
            await smtpClient.ConnectAsync(_mailSettings.Host, _mailSettings.Port, _environment.IsProduction() ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
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

