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
    int bookConntectAdminUserID = 0;

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

        if (PaymentExists(paymentDTO.ID, null, null, null, paymentDTO.Amount))
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
            TransactionStatusResponse? transactionStatusResponse = await _mpesaLibrary.GetTransactionStatusResponse(paymentDTO.ID, tokenResponse.AccessToken, paymentDTO.OrderID);

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

        // TODO: Move this to PostTransactionStatusResult webhook so as to just be sure
        var payment = new Payment
        {
            ID = paymentDTO.ID,
            FromID = int.Parse(userId),
            ToID = bookConntectAdminUserID,
            Amount = paymentDTO.Amount,
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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> PayBookOwner(OrderItemDTO orderItemDTO)
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

        var orderItem = await _context.OrderItems
            .Where(ordItem => ordItem.ID == orderItemDTO.ID)
            .Include(ordItem => ordItem.Book)
            .ThenInclude(ordItem => ordItem.Vendor)
            .FirstOrDefaultAsync();

        if (orderItem == null)
        {
            _logger.LogWarning("Order item not found");
            return NotFound(new { Message = "Order item not found. Try again later." });
        }

        var amount = orderItem.Quantity * orderItem.Book.Price;
        if (PaymentExists(null, orderItem.OrderID, orderItem.Book.VendorID, bookConntectAdminUserID, amount))
        {
            return Conflict(new { Message = "Payment already made to book owner " });
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
            _logger.LogError(ex, "Error getting MPESA Auth token");
            return StatusCode(500, ex.Message);
        }


        _logger.LogInformation("Making B2C request");
        TransactionStatusResponse? transactionStatusResponse = await _mpesaLibrary.MakeBusinessPayment(amount.ToString()!, orderItem.Book.Vendor.Phone!, tokenResponse.AccessToken, orderItem.OrderID);

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
        _logger.LogInformation("B2C request was accepted for processing {0}", transactionStatusResponse);

        return Ok(new { Message = "MPESA request was received. Payment will be made shortly."});

    }

    // webhook to listen to Mpesa B2C result
    [HttpPost("/b2c/result")]
    public void PostB2CResult(JsonResult result)
    {
        // Success, create payment
        Console.WriteLine(result);
    }

    private bool UserExists(int id) => _context.Users.Any(user => user.ID == id);

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
