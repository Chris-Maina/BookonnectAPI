using BookonnectAPI.Configuration;
using BookonnectAPI.Lib;
using BookonnectAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BookonnectAPI.Controllers;

[ApiController]
[Route("/api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class EmailController: ControllerBase
{
    private readonly IMailLibrary _mailLibrary;
    private readonly ILogger<EmailController> _logger;
    private readonly MailSettingsOptions _mailSettings;

    public EmailController(IMailLibrary mailLibrary, ILogger<EmailController> logger, IOptionsSnapshot<MailSettingsOptions> mailSettings)
	{
        _mailLibrary = mailLibrary;
        _logger = logger;
        _mailSettings = mailSettings.Value;

    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> PostEmail(Email email)
    {
        try
        {
            _logger.LogInformation($"Sending email from {email.FromId}");
            email.ToId = _mailSettings.EmailId;
            email.Name = _mailSettings.Name;
            await _mailLibrary.SendMail(email);
            return Ok(new { Message = "Successfully sent email" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving sending email");
            return StatusCode(500, ex.Message);
        }

    }

}

