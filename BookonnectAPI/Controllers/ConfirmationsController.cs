using System.Security.Claims;
using BookonnectAPI.Configuration;
using BookonnectAPI.Data;
using BookonnectAPI.Lib;
using BookonnectAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BookonnectAPI.Controllers;

[Route("/api/[controller]")]
[ApiController]
[Authorize]
public class ConfirmationsController: ControllerBase
{
	private readonly BookonnectContext _context;
	private readonly ILogger<ConfirmationsController> _logger;
    private readonly IMailLibrary _mailLibrary;
    private readonly MailSettingsOptions _mailSettings;

    public ConfirmationsController(BookonnectContext context, ILogger<ConfirmationsController> logger, IMailLibrary mailLibrary, IOptionsSnapshot<MailSettingsOptions> mailSettings)
	{
		_context = context;
		_logger = logger;
		_mailLibrary = mailLibrary;
        _mailSettings = mailSettings.Value;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<Confirmation>> PostConfirmation([FromBody] ConfirmationDTO confirmationDTO)
    {
        _logger.LogInformation("Creating a confirmation");
        var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            _logger.LogWarning("There is no user id in token");
            return Unauthorized(new { Message = "Please sign in again." });
        }

        if (!UserExists(int.Parse(userId)))
        {
            _logger.LogWarning("User in token does not exist");
            return NotFound(new { Message = "User not found. Sign in again." });
        }

        var orderItem = await _context.OrderItems
              .Where(orderItem => orderItem.ID == confirmationDTO.OrderItemID)
              .Include(orderItem => orderItem.Book)
              .ThenInclude(bk => bk != null ? bk.Vendor : null)
              .Include(orderItem => orderItem.Order)
              .ThenInclude(ord => ord != null ? ord.Customer : null)
              .FirstOrDefaultAsync();

        if (orderItem == null)
        {
            _logger.LogError($"Order item with id {confirmationDTO.OrderItemID} not found");
            return NotFound(new { Message = "Order item not found." });
        }

        var confirmationExist = _context.Confirmations.Any(conf => conf.UserID == int.Parse(userId) && conf.Type == confirmationDTO.Type && conf.OrderItemID == confirmationDTO.OrderItemID);
        if (confirmationExist)
        {
            _logger.LogWarning($"Confirmation of order item {confirmationDTO.OrderItemID} with {confirmationDTO.Type} exists");
            return Conflict(new { Message = $"Confirmation of order item {confirmationDTO.OrderItemID} with {confirmationDTO.Type} exists" });
        }

        var confirmation = new Confirmation
        {
            DateTime = DateTime.Now,
            UserID = int.Parse(userId),
            OrderItemID = confirmationDTO.OrderItemID,
            Type = confirmationDTO.Type
        };
        _context.Confirmations.Add(confirmation);

        try
        {
            _logger.LogInformation("Saving confirmation to database");
            await _context.SaveChangesAsync();
            SendEmail(confirmation.Type, orderItem);
            return CreatedAtAction(nameof(GetConfirmation), new { id = confirmation.ID }, confirmation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, ex.Message);
        }
    }

    // GET api/<ConfirmationsController>/5
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<Confirmation>> GetConfirmation(int id)
    {
        _logger.LogInformation("Get confirmation");
        var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            _logger.LogWarning("There is no user id in token");
            return Unauthorized(new { Message = "Please sign in again." });
        }

        if (!UserExists(int.Parse(userId)))
        {
            _logger.LogWarning("User in token does not exist");
            return NotFound(new { Message = "User not found. Sign in again." });
        }

        var confirmation = await _context.Confirmations.FindAsync(id);

        if (confirmation == null)
        {
            return NotFound(new { Message = "Confirmation not found " });
        }

        return Ok(confirmation);
    }


    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> PatchConfirmation(int id, [FromBody] JsonPatchDocument<Confirmation> patchDoc)
	{
		_logger.LogInformation("Updating confirmation");
        var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            _logger.LogWarning("There is no user id in token");
            return Unauthorized(new { Message = "Please sign in again." });
        }

        if (!UserExists(int.Parse(userId)))
        {
            _logger.LogWarning("User in token does not exist");
            return NotFound(new { Message = "User not found. Sign in again." });
        }

        if (patchDoc == null)
		{
			return BadRequest(ModelState);
		}

		var confirmation = await _context.Confirmations.FindAsync(id);
		if (confirmation == null)
		{
            _logger.LogWarning("Confirmation not found");
            return NotFound(new { Message = "Confirmation not found" });
		}

        patchDoc.ApplyTo(confirmation, ModelState);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _context.Update(confirmation);

        try
        {
            await _context.SaveChangesAsync();
            return Ok(confirmation);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Error saving confirmation to DB");
            if (!ConfirmationExists(id))
            {
                return NotFound(new { Message = "Confirmation not found" });
            }
            else
            {
                return StatusCode(500, ex.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving confirmation to DB");
            return StatusCode(500, ex.Message);
        }


    }

    private bool UserExists(int id) => _context.Users.Any(u => u.ID == id);

    private bool ConfirmationExists(int id) => _context.Confirmations.Any(u => u.ID == id);

    private void SendEmail(ConfirmationType confirmationType, OrderItem orderItem)
    {
        switch (confirmationType)
        {
            case ConfirmationType.Dispatch:
                if (orderItem.Order?.Customer == null)
                {
                    _logger.LogWarning("Order customer not found");
                    break;
                }
                if (orderItem.Book == null)
                {
                    _logger.LogWarning("Ordered book not found");
                    break;
                }

                // Send customer dispatched email
                SendDispatchEmail(orderItem.Order.Customer, orderItem.Book);
                break;
            case ConfirmationType.Receipt:
                if (orderItem.Book?.Vendor == null)
                {
                    _logger.LogWarning("Book vendor not found");
                    break;
                }
                // Send vendor receipt email by customer
                SendReceiptEmail(orderItem.Book.Vendor, orderItem.Book);
                // Send payment request to admin
                float amount = (float)(orderItem.Quantity * orderItem.Book.Price);
                SendBookVendorPaymentRequest(orderItem.ID, orderItem.Book, amount);
                break;
        }
    }

    private void SendDispatchEmail(User receiver, Book book)
    {
        var emailData = new Email
        {
            Subject = $"{book.Title} dispatched!",
            ToId = receiver.Email,
            Name = receiver.Name,
            Body = $@"<html><body>
                        <p>Hi {receiver.Name},</p>
                        <p>We've got good news! Your ordered book, {book.Title}, has been dispatched by the vendor.</p>
                        <p> Please update the status upon receipt under My Orders.</p>

                        <p>Warm regards,</p>
                        <p>Bookonnect Team.</p>"
        };

        _logger.LogInformation($"Sending dispatch email to {receiver.Name}");
        _mailLibrary.SendMail(emailData);
    }

    private void SendReceiptEmail(User receiver, Book book)
    {
        var emailData = new Email
        {
            Subject = $"{book.Title} delivered successfully!",
            ToId = receiver.Email,
            Name = receiver.Name,
            Body = $@"<html><body>
                        <p>Hi {receiver.Name},</p>
                        <p>We've got good news! Customer has confirmed receipt of the book, {book.Title}.</p>
                        <p>We'll be sending your payment shortly.</p>
                        <p>If payment delays for a day, send a reminder to Bookonnect Admin using the email in from field.</p>

                        <p>Warm regards,</p>
                        <p>Bookonnect Team.</p>"
        };

        _logger.LogInformation($"Sending receipt email to {receiver.Name}");
        _mailLibrary.SendMail(emailData);
    }

    private void SendBookVendorPaymentRequest(int orderItemID, Book book, float amount)
    {
        var emailData = new Email
        {
            Subject = $"Send Payment for Order Item {orderItemID}",
            ToId = _mailSettings.EmailId,
            Name = _mailSettings.Name,
            Body = $@"<html>
                        <body>
                            <p>Send payment for {book.Title} to vendor with details</p>
                            <ul>
                               <li>Phone number: <b>{book.Vendor.Phone}</b></li>
                               <li>Amount: <b>{amount}</b></li>
                               <li>Name: <b>{book.Vendor.Name}</b></li>
                            </ul>

                            <p>Warm regards,</p>
                            <p>Bookonnect Team.</p>
                        </body>
                    "
        };
        _logger.LogInformation($"Sending payment request email to Bookonnect Admin to pay vendor");
        _mailLibrary.SendMail(emailData);
    }
}

