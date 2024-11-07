using System.Security.Claims;
using BookonnectAPI.Data;
using BookonnectAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookonnectAPI.Lib;
using Microsoft.EntityFrameworkCore;

namespace BookonnectAPI.Controllers;

[Route("/api/[controller]")]
[ApiController]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly BookonnectContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PaymentsController> _logger;
    private readonly IMpesaLibrary _mpesaLibrary;

    public PaymentsController(BookonnectContext context, IHttpClientFactory httpClientFactory, ILogger<PaymentsController> logger, IMpesaLibrary mpesaLibrary)
    {
        _context = context;
        _context.Database.EnsureCreated();
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _mpesaLibrary = mpesaLibrary;
    }

    // POST: api/Payments
    // Send payment to MPESA to get confirmation
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

        if (!UserExists(int.Parse(userId)))
        {
            _logger.LogWarning("User with the provided id not found");
            return NotFound(new { Message = "User not found. Sign in again." });
        }

        var existingPayment = await _context.Payments
            .Where(p => p.ID == paymentDTO.ID && p.OrderID == paymentDTO.OrderID && p.UserID == int.Parse(userId))
            .Include(p => p.User)
            .FirstOrDefaultAsync();

        if (PaymentExists(paymentDTO.ID, paymentDTO.OrderID, int.Parse(userId)))
        {
            _logger.LogInformation("Found an existing payment with the ID {0}", paymentDTO.ID);
            return Conflict(new { Message = "Payment already exists" });
        }

        try
        {
            // Add token in cache with user id as key. If expired refetch
            _logger.LogInformation("Fetching MPESA access token");
            MpesaAuthToken? tokenResponse = await _mpesaLibrary.GetMpesaAuthToken();

            if (tokenResponse == null)
            {
                _logger.LogWarning("MPESA access token not found");
                return NotFound(new { Message = "Could not fetch MPESA auth token. Try again later." });
            }

            _logger.LogInformation("Fetching transaction status");
            TransactionStatusResponse? transactionStatusResponse = await _mpesaLibrary.GetTransactionStatusResponse(paymentDTO.ID, tokenResponse.AccessToken);

            if (transactionStatusResponse == null)
            {
                _logger.LogWarning("Could not find transaction for payment with key {0}", paymentDTO.ID);
                return NotFound(new { Message = "Could not find transaction with the provided MPESA code" });
            }
            if (transactionStatusResponse.ResponseCode != 0)
            {
                _logger.LogError("The payment key {0} has the tranaction error {1}", paymentDTO.ID, transactionStatusResponse);
                return BadRequest(new { Message = "MPESA code has an error. Check and try again." });
            }
            _logger.LogInformation("Received successfull transaction status {0}", transactionStatusResponse);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending MPESA request");
            return StatusCode(500, ex.Message);
        }

        var payment = new Payment
        {
            ID = paymentDTO.ID,
            UserID = int.Parse(userId),
            DateTime = DateTime.Now,
            OrderID = paymentDTO.OrderID
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

    // webhook to listen to Mpesa transaction status result
    [HttpPost("/transactionstatus/result")]
    public void PostTransactionStatusResult(JsonResult result)
    {
        // Check the DebitPartyName includes the business
        // find order with the authorised user id, status(OrderPlaced) and payment amount  
        // if not found means the transaction id is invalid. 400 BadRequest.
        // Success, create payment
        Console.WriteLine(result);
    }

    [HttpPost("/owner")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> PayBookOwner(PaymentDTO paymentDTO)
    {
        var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            _logger.LogWarning("User id not found in token");
            return Unauthorized(new { Message = "Please sign in again." });
        }

        if (!UserExists(int.Parse(userId)))
        {
            _logger.LogWarning("User with the provided id not found");
            return NotFound(new { Message = "User not found. Sign in again." });
        }

        var paymentsWithOrdersCount = await _context.Payments
            .Where(p => p.OrderID == paymentDTO.OrderID)
            .CountAsync();

        if (paymentsWithOrdersCount >= 2)
        {
            return Conflict(new { Message = "Payment already made to book owner" });
        }


        var order = await _context.Orders
            .Where(ord => ord.ID == paymentDTO.OrderID)
            .Include(ord => ord.OrderItems)
            .ThenInclude(ordItem => ordItem.Book)
            .ThenInclude(bk => bk != null ? bk.Vendor : null)
            .FirstOrDefaultAsync();

        if (order == null)
        {
            return NotFound(new { Message = "Order not found" });
        }

        MpesaAuthToken? tokenResponse;
        try
        {
            _logger.LogInformation("Fetching MPESA access token");
            tokenResponse = await _mpesaLibrary.GetMpesaAuthToken();

            if (tokenResponse == null)
            {
                _logger.LogWarning("MPESA access token not found");
                return NotFound(new { Message = "Could not fetch MPESA auth token. Try again later." });
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending MPESA request");
            return StatusCode(500, ex.Message);
        }


        foreach (var orderItem in order.OrderItems)
        {
            if (PaymentExists(null, order.ID, orderItem.Book?.VendorID))
            {
                return Conflict(new { Message = "Payment already made to book owner " });
            }

            _logger.LogInformation("Making B2C request");
            var amount = orderItem.Quantity * orderItem.Book?.Price;
            TransactionStatusResponse? transactionStatusResponse = await _mpesaLibrary.MakeBusinessPayment(amount.ToString()!, orderItem.Book?.Vendor.Phone!, tokenResponse.AccessToken, paymentDTO.OrderID);

            if (transactionStatusResponse == null)
            {
                _logger.LogWarning("Mpesa b2 request had empty response");
                return NotFound(new { Message = "Received null response from MPESA" });
            }
            if (transactionStatusResponse.ResponseCode != 0)
            {
                _logger.LogError("The B2C request has the error {0}", transactionStatusResponse);
                return BadRequest(new { Message = "MPESA business payment request was unsuccessfull. Try again later." });
            }
            _logger.LogInformation("B2C request was successfull {0}", transactionStatusResponse);
        }

        return Ok();

    }

    // webhook to listen to Mpesa B2C result
    [HttpPost("/b2c/result")]
    public void PostB2CResult(JsonResult result)
    {
        // Success, create payment
        Console.WriteLine(result);
    }

    private bool UserExists(int id) => _context.Users.Any(user => user.ID == id);

    private bool PaymentExists(string? id, int? orderID, int? userID) 
    {
        if (id != null)
        {
            return _context.Payments.Any(p => p.ID == id);
        }

        if (orderID != null && userID != null)
        {

            return _context.Payments.Any(p => p.OrderID == orderID && p.UserID == userID);
        }

        return false;
    }
}
