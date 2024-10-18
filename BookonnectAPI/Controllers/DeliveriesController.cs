using System.Security.Claims;
using BookonnectAPI.Data;
using BookonnectAPI.Lib;
using BookonnectAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookonnectAPI.Controllers;

[ApiController]
[Authorize]
[Route("/api/[controller]")]
public class DeliveriesController : ControllerBase
{
    private readonly BookonnectContext _context;
    private readonly ILogger<DeliveriesController> _logger;
    private readonly IMailLibrary _mailLibrary;
    public DeliveriesController(BookonnectContext context, ILogger<DeliveriesController> logger, IMailLibrary mailLibrary)
    {
        _context = context;
        _context.Database.EnsureCreated();
        _logger = logger;
        _mailLibrary = mailLibrary;
    }

    // GET: api/deliveries
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<DeliveryDTO>>> GetDeliveries([FromQuery] QueryParameter queryParameter)
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

        DeliveryDTO[] deliveries = await _context.Deliveries
            .Where(d => d.UserID == int.Parse(userId))
            .Take(queryParameter.Size)
            .Select(d => Delivery.DeliveryToDTO(d))
            .ToArrayAsync();

        return Ok(deliveries);
    }

    // GET api/deliveries/5
    [HttpGet("{id}")]
    public ActionResult<DeliveryDTO> Get(int id)
    {
        return Ok(new DeliveryDTO());
    }

    // POST api/deliveries
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DeliveryDTO>> PostDelivery([FromBody] DeliveryDTO deliveryDTO)
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

        var deliveryExists = _context.Deliveries.Any(d =>
            d.Location == deliveryDTO.Location &&
            d.UserID == int.Parse(userId));

        if (deliveryExists)
        {
            _logger.LogWarning("Delivery exists");
            return Conflict(new { Message = "Delivery already exists" });
        }


        _logger.LogInformation("Creating delivery");
        var delivery = new Delivery
        {
            Location = deliveryDTO.Location,
            Instructions = deliveryDTO.Instructions,
            UserID = int.Parse(userId),
        };

        _context.Deliveries.Add(delivery);
        try
        {
            
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(PostDelivery), Delivery.DeliveryToDTO(delivery));
        } catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving delivery to DB");
            return StatusCode(500, ex.Message);
        }
    }

    // PUT api/deliveries/5
    [HttpPut("{id}")]
    public void PutDelivery(int id, [FromBody]string value)
    {
    }

    // PATCH api/deliveries/5
    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DeliveryDTO>> PatchDelivery(int id, [FromBody] JsonPatchDocument<Delivery> patchDocument)
    {
        if (patchDocument == null)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Fetching delivery with id {0}", id);
        var delivery = await _context.Deliveries
             .Include(d => d.User)
             .FirstOrDefaultAsync(d => d.ID == id);


        if (delivery == null)
        {
            _logger.LogWarning("Delivery with the specified id {0} not found", id);
            return NotFound(new { Message = "Delivery with provided id does not exist" });
        }


        patchDocument.ApplyTo(delivery, ModelState);
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _context.Update(delivery);
        try
        {
            await _context.SaveChangesAsync();

            // Send an email to book owner to deliver book. Details will include location and instructions
            SendMail(delivery, delivery.User!);
            // Add a button action to update delivery status once owner has shipped book
            return Ok(Delivery.DeliveryToDTO(delivery));
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (!DeliveryExists(id))
            {
                _logger.LogError("Delivery with id {0} does not exist", id);
                return NotFound(new { Message = "Delivery not found" });
            }
            else
            {
                _logger.LogError(ex.Message);
               return StatusCode(500, ex.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return StatusCode(500, ex.Message);
        }
    }

    // DELETE api/deliveries/5
    [HttpDelete("{id}")]
    public void DeleteDelivery(int id)
    {
    }

    private bool DeliveryExists(int id)
    {
        return _context.CartItems.Any(e => e.ID == id);
    }

    private bool UserExists(int id) => _context.Users.Any(user => user.ID == id);

    private async Task<Order?> GetOrder(Delivery delivery)
    {
        return await _context.Orders
                .Where(ord => ord.DeliveryID == delivery.ID)
                .Include(ord => ord.OrderItems)
                .ThenInclude(oi => oi.Book)
                .ThenInclude(b => b != null ? b.User : null)
                .FirstOrDefaultAsync();
    }

    private async void SendMail(Delivery delivery, User recipient)
    {
        Order? order;
        switch(delivery.Status)
        {
            case DeliveryStatus.OrderConfirmed:
                order = await GetOrder(delivery);
                if (order == null)
                {
                    break;
                }
                // loop through order items to find book owners
                foreach (OrderItem orderItem in order.OrderItems)
                {
                    if (orderItem.Book != null)
                    {
                        SendDeliverEmail(orderItem.Book.User, delivery);
                    }
                }
                break;
            case DeliveryStatus.InTransit:
                SendInTransitEmail(recipient);
                break;
            case DeliveryStatus.Delivered:
                order = await GetOrder(delivery);
                if (order == null)
                {
                    break;
                }
                // loop through order items to find book owners
                foreach (OrderItem orderItem in order.OrderItems)
                {
                    if (orderItem.Book != null)
                    {
                        SendDeliverySuccessfulEmail(orderItem.Book.User);
                    }
                }
                break;
            default:
                break;
        }
    }


    private void SendDeliverEmail(User owner, Delivery delivery)
    {
        var emailData = new Email
        {
            ToId = owner.Email,
            Name = owner.Name,
            Subject = "Deliver book",
            Body = $@"<p>Hi {owner.Name},</p>
                <p>Please deliver the book using the details below:</p>
                <ul>
                    <li>To {delivery.User?.Name} </li>
                    <li>{delivery.Location}</li>
                    <li>{delivery.Instructions}</li>
                </ul>"
        };
        _mailLibrary.SendMail(emailData);
    }

    private void SendInTransitEmail(User recipient)
    {
        var emailData = new Email
        {
            ToId = recipient.Email,
            Name = recipient.Name,
            Subject = "Delivery in transit",
            Body = $@"<p>Hi {recipient.Name},</p>
                <p>Your book has been dispatched for delivery with the details you provided. Please be patient</p>
                <p>Warm regards,</p>
                <p>Bookonnect Team </p>
                "
        };
        _mailLibrary.SendMail(emailData);
    }

    private void SendDeliverySuccessfulEmail(User owner)
    {
        var emailData = new Email
        {
            ToId = owner.Email,
            Name = owner.Name,
            Subject = "Book delivered!",
            Body = $@"<p>Hi {owner.Name},</p>
                <p>The book has been successfully delivered. Payment will be made within 24 hours</p>
                <p>Warm regards,</p>
                <p>Bookonnect Team </p>
                "
        };
        _mailLibrary.SendMail(emailData);
    }
}

