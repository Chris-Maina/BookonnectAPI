using System.Security.Claims;
using System.Text;
using System.Text.Json;
using BookonnectAPI.Data;
using BookonnectAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static System.Net.Mime.MediaTypeNames;

namespace BookonnectAPI.Controllers;

[Route("/api/[controller]")]
[ApiController]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly BookonnectContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(BookonnectContext context, IHttpClientFactory httpClientFactory, ILogger<PaymentsController> logger)
    {
        _context = context;
        _context.Database.EnsureCreated();
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // Send payment to MPESA to get confirmation
    [HttpPost]
    public async Task<ActionResult<string>> Post(Payment payment)
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

        string response = string.Empty;
        try
        {
           response = await ConfirmMpesaPayment(payment);
           Console.WriteLine(response);
           _logger.LogInformation("Received MPESA confirmation {0}", response);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error sending MPESA request");
            throw;
        }

        if (string.IsNullOrEmpty(response))
        {
            _logger.LogWarning("Could not find MPESA payment with key {0}", payment.ID);
            return NotFound();
        }

        // Serialize response to get datetime and phone number
        payment.DateTime = DateTime.Now;
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


    private async Task<string> ConfirmMpesaPayment(Payment payment)
    {

        // construct mpesa request and send it
        var payload = new
        {
            ShortCode = 7845640,
            CommandID = "CustomerBuyGoodsOnline",
            Amount = payment.Order.Total,
            Msisdn = payment.Phone,
            BillRefNumber = "null",
        };
        var paymentJson = new StringContent(
        JsonSerializer.Serialize(payload),
        Encoding.UTF8,
        Application.Json);

        var httpClient = _httpClientFactory.CreateClient("Safaricom");
        using var response = await httpClient.PostAsync("/mpesa/c2b/v1/simulate", paymentJson);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
