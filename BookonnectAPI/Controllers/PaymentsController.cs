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

[Route("/api/[controller]")]
[ApiController]
[Authorize(Policy = "UserClaimPolicy")]
public class PaymentsController : ControllerBase
{
    private readonly BookonnectContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PaymentsController> _logger;
    private readonly MailSettingsOptions _mailSettings;
    private readonly IMailLibrary _mailLibrary;

    public PaymentsController(BookonnectContext context, IHttpClientFactory httpClientFactory, ILogger<PaymentsController> logger, IOptionsSnapshot<MailSettingsOptions> mailSettings, IMailLibrary mailLibrary)
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
        var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            _logger.LogWarning("User id not found in token");
            return Unauthorized(new { Message = "Please sign in again." });
        }

        var order = await _context.Orders.FindAsync(paymentDTO.OrderID);
        if (order == null)
        {
            _logger.LogInformation("Order with id {0} not found", paymentDTO.OrderID);
            return NotFound(new { Message = "Order not found. Try again." });
        }

        if (PaymentExists(paymentDTO.ID, null, null, null, order.Total))
        {
            _logger.LogInformation("Found an existing payment with the ID {0}", paymentDTO.ID);
            return Conflict(new { Message = "Payment already exists" });
        }

        var bookonnectAdmin = _context.Users.Where(u => u.Email == _mailSettings.EmailId).FirstOrDefault();
        if (bookonnectAdmin == null)
        {
            return NotFound(new { Message = "Could not find Bookonnect admin payment details.Try again" });
        }

        // Send BookAdmin notification to verify payment. List MPESA ref & amount and/or timestamp 
        var payment = new Payment
        {
            ID = paymentDTO.ID,
            FromID = int.Parse(userId),
            ToID = bookonnectAdmin.ID,
            Amount = order.Total,
            DateTime = DateTime.Now,
            OrderID = paymentDTO.OrderID
        };
        _context.Payments.Add(payment);

        try
        {
            await _context.SaveChangesAsync();
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
        var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            _logger.LogWarning("There is no user id in token");
            return Unauthorized(new { Message = "Please sign in" });
        }

        var bookonnectAdmin = _context.Users.Where(u => u.ID == int.Parse(userId) && u.Email == _mailSettings.EmailId).FirstOrDefault();
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

        try
        {
            await _context.SaveChangesAsync();
            return Ok(payment);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Error saving payment to DB");
            if (!PaymentExists(id, null, null, null, null))
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
    // Sends payment request to Mpesa
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
        var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            _logger.LogWarning("User id not found in token");
            return Unauthorized(new { Message = "Please sign in again." });
        }

        var bookonnectAdmin = _context.Users.Where(u => u.ID == int.Parse(userId) && u.Email == _mailSettings.EmailId).FirstOrDefault();
        if (bookonnectAdmin == null)
        {
            return NotFound(new { Message = "Functionality only availabile for Admin user " });
        }

        var orderItem = await _context.OrderItems
            .Where(ordItem => ordItem.ID == paymentDTO.OrderItemID)
            .Include(ordItem => ordItem.Book)
            .ThenInclude(bk => bk != null ? bk.Vendor :null)
            .FirstOrDefaultAsync();

        if (orderItem == null || orderItem.Book == null)
        {
            _logger.LogWarning("Order item not found");
            return NotFound(new { Message = "Order item not found. Try again later." });
        }

        if (PaymentExists(null, orderItem.OrderID, orderItem.Book.VendorID, bookonnectAdmin.ID, paymentDTO.Amount))
        {
            return Conflict(new { Message = "Payment already made to book owner " });
        }

        var payment = new Payment
        {
            ID = paymentDTO.ID,
            FromID = bookonnectAdmin.ID,
            ToID = orderItem.Book.VendorID,
            Amount = paymentDTO.Amount,
            DateTime = paymentDTO.DateTime,
            OrderID = orderItem.OrderID,
            Status = PaymentStatus.Verified,
        };

        _context.Payments.Add(payment);

        try
        {
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
        var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            _logger.LogWarning("User id not found in token");
            return Unauthorized(new { Message = "Please sign in again." });
        }

        var bookonnectAdmin = _context.Users.Where(u => u.ID == int.Parse(userId) && u.Email == _mailSettings.EmailId).FirstOrDefault();
        if (bookonnectAdmin == null)
        {
            return NotFound(new { Message = "Functionality only availabile for Admin user " });
        }

        var payments = await _context.Payments
               .OrderBy(p => p.ID)
               .Select(p => Payment.PaymentToDTO(p))
               .ToArrayAsync();

        return Ok(payments);
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

    private bool PaymentExists(string? id, int? orderID, int? toUserID, int? fromUserID, float? amount) 
    {
        if (id != null)
        {
            return _context.Payments.Any(p => p.ID == id);
        }

        if (orderID != null && toUserID != null && fromUserID != null && amount != null)
        {

            return _context.Payments.Any(p => p.OrderID == orderID && p.ToID == toUserID && p.FromID == fromUserID && p.Amount == amount);
        }

        return false;
    }
}
