using System.Security.Claims;
using BookonnectAPI.Data;
using BookonnectAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookonnectAPI.Lib;
using Microsoft.EntityFrameworkCore;
using BookonnectAPI.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.JsonPatch;

namespace BookonnectAPI.Controllers;

[Route("/api/v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize(Policy = "UserClaimPolicy")]
[ApiVersion("1.0")]
public class PaymentsController : ControllerBase
{
    private readonly BookonnectContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PaymentsController> _logger;
    private readonly MailSettingsOptions _mailSettings;
    private readonly IMailLibrary _mailLibrary;

    public PaymentsController(BookonnectContext context, IHttpClientFactory httpClientFactory, ILogger<PaymentsController> logger, IOptions<MailSettingsOptions> mailSettings, IMailLibrary mailLibrary)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _mailSettings = mailSettings.Value;
        _mailLibrary = mailLibrary;
    }

    // POST: api/Payments
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<PaymentDTO>> PostPayment(PaymentDTO paymentDTO)
    {

        try
        {
            var order = await _context.Orders.FindAsync(paymentDTO.OrderID);
            if (order == null)
            {
                _logger.LogInformation("Order with id {0} not found", paymentDTO.OrderID);
                return NotFound(new { Message = "Order not found. Try again." });
            }

            bool paymentExists = await PaymentExists(paymentDTO.ID, paymentDTO.OrderID, null, null, order.Total);
            if (paymentExists)
            {
                _logger.LogInformation("Found an existing payment with the ID {0}", paymentDTO.ID);
                return Conflict(new { Message = "Payment already exists" });
            }

            var bookonnectAdmin = await _context.Users.Where(u => u.Email == _mailSettings.EmailId).FirstOrDefaultAsync();
            if (bookonnectAdmin == null)
            {
                return NotFound(new { Message = "Could not find some payment details.Try again later" });
            }

            int.TryParse(this.User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId);
            var payment = new Payment
            {
                ID = paymentDTO.ID,
                FromID = userId,
                ToID = bookonnectAdmin.ID,
                Amount = order.Total,
                DateTime = DateTime.Now, // Should reflect mpesa payment date, payment.DTO???
                OrderID = paymentDTO.OrderID
            };
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            // Send BookAdmin notification to verify payment. List MPESA ref & amount and/or timestamp
            SendPaymentVerificationRequest(payment);
            return CreatedAtAction(nameof(PostPayment), new { id = payment.ID }, Payment.PaymentToDTO(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving payment in DB");
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> PatchPayment(string id, [FromBody] JsonPatchDocument<Payment> patchDoc)
    {
        _logger.LogInformation("Updating payment");
        try
        {
            int.TryParse(this.User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId);

            var bookonnectAdmin = await _context.Users.Where(u => u.ID == userId && u.Email == _mailSettings.EmailId).FirstOrDefaultAsync();
            if (bookonnectAdmin == null)
            {
                return NotFound(new { Message = "Functionality only availabile for Admin user " });
            }

            if (patchDoc == null)
            {
                return BadRequest(ModelState);
            }

            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
                _logger.LogWarning("Payment not found");
                return NotFound(new { Message = "Payment not found" });
            }

            patchDoc.ApplyTo(payment, ModelState);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Update(payment);

        
            await _context.SaveChangesAsync();
            return Ok(payment);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Error saving payment to DB");
            bool paymentExists = await PaymentExists(id, null, null, null, null);
            if (!paymentExists)
            {
                return NotFound(new { Message = "Payment not found" });
            }
            else
            {
                return StatusCode(500, ex.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving payment to DB");
            return StatusCode(500, ex.Message);
        }

    }


    // POST: api/Payments/owner
    // Sends payment request to book owner
    // Functionality only available to admin
    [HttpPost]
    [Route("Owner")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> PayBookOwner(PaymentOwnerBody paymentDTO)
    {
        try
        {
            int.TryParse(this.User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId);

            var bookonnectAdmin = await _context.Users
                .Where(u => u.ID == userId && u.Email == _mailSettings.EmailId)
                .FirstOrDefaultAsync();
            if (bookonnectAdmin == null)
            {
                return NotFound(new { Message = "Functionality only availabile for Admin user " });
            }

            var orderItem = await _context.OrderItems
                .Where(ordItem => ordItem.ID == paymentDTO.OrderItemID)
                .Include(ordItem => ordItem.Book)
                .ThenInclude(bk => bk != null && bk.OwnedDetails != null ? bk.OwnedDetails.Vendor :null)
                .FirstOrDefaultAsync();

            if (orderItem == null || orderItem.Book == null || orderItem.Book.OwnedDetails == null)
            {
                _logger.LogWarning("Order item not found");
                return NotFound(new { Message = "Order item not found. Try again later." });
            }

            var paymentExists = await PaymentExists(null, orderItem.OrderID, orderItem.Book.OwnedDetails.VendorID, bookonnectAdmin.ID, paymentDTO.Amount);
            if (paymentExists)
            {
                return Conflict(new { Message = "Payment already made to book owner " });
            }

            var payment = new Payment
            {
                ID = paymentDTO.ID,
                FromID = bookonnectAdmin.ID,
                ToID = orderItem.Book.OwnedDetails.VendorID,
                Amount = paymentDTO.Amount,
                DateTime = DateTime.Now, // Should reflect mpesa payment date, payment.DTO???
                OrderID = orderItem.OrderID,
                Status = PaymentStatus.Verified,
            };

            _context.Payments.Add(payment);

        
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(PostPayment), new { id = payment.ID }, Payment.PaymentToDTO(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving payment in DB");
            return StatusCode(500, ex.Message);
        }

    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetPayments()
    {
        _logger.LogInformation("Getting payments");
        try
        {
            int.TryParse(this.User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId);

            var bookonnectAdmin = await _context.Users
                .Where(u => u.ID == userId && u.Email == _mailSettings.EmailId)
                .FirstOrDefaultAsync();
            if (bookonnectAdmin == null)
            {
                return NotFound(new { Message = "Functionality only availabile for Admin user " });
            }

            var payments = await _context.Payments
                   .OrderBy(p => p.ID)
                   .Select(p => Payment.PaymentToDTO(p))
                   .ToArrayAsync();

            return Ok(payments);
        } catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving payment in DB");
            return StatusCode(500, ex.Message);
        }
    }

    private void SendPaymentVerificationRequest(Payment payment)
    {
        var emailData = new Email
        {
            Subject = $"Confirm Payment for Order {payment.OrderID}",
            ToId = _mailSettings.EmailId,
            Name = _mailSettings.Name,
            Body = $@"<html>
                        <body>
                            <p>Confirm the payment:</p>
                            <ul>
                               <li>Mpesa ref: <b>{payment.ID}</b></li>
                               <li>Amount: <b>{payment.Amount}</b></li>
                               <li>Time: <b>{payment.DateTime}</b></li>
                            </ul>

                            <p>Warm regards,</p>
                            <p>Bookonnect Team.</p>
                        </body>
                    "
        };
        _logger.LogInformation($"Sending confirm payment email to Bookonnect Admin ");
        _mailLibrary.SendMail(emailData);
    }

    private async Task<bool> PaymentExists(string? id, int? orderID, int? toUserID, int? fromUserID, float? amount) 
    {
        if (id != null && orderID != null)
        {
            return await _context.Payments.AnyAsync(p => p.ID == id && p.OrderID == orderID);
        }

        if (id != null)
        {
            return await _context.Payments.AnyAsync(p => p.ID == id);
        }

        if (orderID != null && toUserID != null && fromUserID != null && amount != null)
        {

            return await _context.Payments.AnyAsync(p => p.OrderID == orderID && p.ToID == toUserID && p.FromID == fromUserID && p.Amount == amount);
        }

        return false;
    }
}
