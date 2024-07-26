using System.Security.Claims;
using BookonnectAPI.Data;
using BookonnectAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace BookonnectAPI.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class DeliveriesController : ControllerBase
{
    private readonly BookonnectContext _context;
    private readonly ILogger<DeliveriesController> _logger;
    public DeliveriesController(BookonnectContext context, ILogger<DeliveriesController> logger)
    {
        _context = context;
        _context.Database.EnsureCreated();
        _logger = logger;
    }

    // GET: api/deliveries
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DeliveryDTO>>> Get()
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

        var deliveries = _context.Deliveries
            .Where(d => d.UserID == user.ID)
            .Select(Delivery.DeliveryToDTO);
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
    public async Task<ActionResult<DeliveryDTO>> Post([FromBody] DeliveryDTO deliveryDTO)
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

        _logger.LogInformation("Creating delivery");
        var delivery = new Delivery
        {
            Name = deliveryDTO.Name,
            Location = deliveryDTO.Location,
            Phone = deliveryDTO.Phone,
            Instructions = deliveryDTO.Instructions,
            OrderID = deliveryDTO.OrderID,
            Status = deliveryDTO.Status
        };

        _context.Deliveries.Add(delivery);
        try
        {
            
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Post), new DeliveryDTO());
        } catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving delivery to DB");
            throw;
        }
         
       
    }

    // PUT api/deliveries/5
    [HttpPut("{id}")]
    public void Put(int id, [FromBody]string value)
    {
    }

    // DELETE api/deliveries/5
    [HttpDelete("{id}")]
    public void Delete(int id)
    {
    }
}

