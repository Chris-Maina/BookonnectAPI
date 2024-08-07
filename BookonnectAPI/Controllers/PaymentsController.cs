using System.Security.Claims;
using BookonnectAPI.Data;
using BookonnectAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookonnectAPI.Lib;

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

    // Send payment to MPESA to get confirmation
    [HttpPost]
    public async Task<ActionResult<string>> Post(PaymentDTO paymentDTO)
    {
        var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            _logger.LogWarning("User id not found in token");
            return NotFound();
        }

        var user = await _context.Users.FindAsync(int.Parse(userId));
        if (user == null)
        {
            _logger.LogWarning("User with the provided id not found");
            return NotFound();
        }

        try
        {
            _logger.LogInformation("Fetching MPESA access token");
            MpesaAuthToken? tokenResponse = await _mpesaLibrary.GetMpesaAuthToken();

            if (tokenResponse == null)
            {
                _logger.LogWarning("MPESA access token not found");
                return NotFound();
            }

            _logger.LogInformation("Fetching transaction status");
            TransactionStatusResponse? transactionStatusResponse = await _mpesaLibrary.GetTransactionStatusResponse(paymentDTO.ID, tokenResponse.AccessToken);

            if (transactionStatusResponse == null)
            {
                _logger.LogWarning("Could not find transaction for payment with key {0}", paymentDTO.ID);
                return NotFound();
            }
            if (transactionStatusResponse.ResponseCode != 0)
            {
                _logger.LogError("The payment key {0} has the tranaction error {1}", paymentDTO.ID, transactionStatusResponse);
                return BadRequest();
            }
            _logger.LogInformation("Received successfull transaction status {0}", transactionStatusResponse);
           
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error sending MPESA request");
            throw;
        }

        var payment = new Payment
        {
            ID = paymentDTO.ID,
            UserID = user.ID,
            DateTime = DateTime.Now
        };
        _context.Payments.Add(payment);

        try
        {
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Post), new { id = payment.ID }, "Successfully made payment");
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error saving payment in DB");
            throw;
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
}
