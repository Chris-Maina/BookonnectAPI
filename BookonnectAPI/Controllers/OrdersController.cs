using System.Security.Claims;
using BookonnectAPI.Data;
using BookonnectAPI.Lib;
using BookonnectAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookonnectAPI.Controllers
{
    [Route("/api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly BookonnectContext _context;
        private readonly ILogger<OrdersController> _logger;
        private readonly IMailLibrary _mailLibrary;
        public OrdersController(BookonnectContext context, ILogger<OrdersController> logger, IMailLibrary mailLibrary)
        {
            _context = context;
            context.Database.EnsureCreated();
            _logger = logger;
            _mailLibrary = mailLibrary;
        }

        // GET: api/<OrdersController>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetOrders([FromQuery] OrderQueryParameters orderQueryParameters)
        {
            _logger.LogInformation("Getting orders");
            var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                _logger.LogWarning("No token found");
                return Unauthorized(new { Message = "Please sign in again." });
            }

            if (!UserExists(int.Parse(userId)))
            {
                _logger.LogWarning("User in token does not exist");
                return NotFound(new { Message = "User not found. Sign in again." });
            }

            _logger.LogInformation("Fetching orders by logged in user");
            IQueryable<OrderDTO> orders;
            if (orderQueryParameters.Total != null)
            {
                orders = _context.Orders
                    .Where(ord =>
                        ord.CustomerID == int.Parse(userId) &&
                        ord.Total == orderQueryParameters.Total)
                    .Include(ord => ord.Customer)
                    .Include(ord => ord.Payments)
                    .Include(ord => ord.OrderItems)
                    .ThenInclude(orderItem => orderItem.Book)
                    .Select(ord => Order.OrderToDTO(ord));

                return Ok(await orders.ToArrayAsync());
            }
            orders = _context.Orders
                .Where(ord => ord.CustomerID == int.Parse(userId))
                .Include(ord => ord.Customer)
                .Include(ord => ord.Payments)
                .Include(ord => ord.OrderItems)
                .ThenInclude(orderItem => orderItem.Book)
                .Select(ord => Order.OrderToDTO(ord));

            return Ok(await orders.ToArrayAsync());
        }

        // GET api/<OrdersController>/5
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<OrderDTO>> GetOrder(int id)
        {
            _logger.LogInformation("Getting order");
            var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                _logger.LogWarning("No token found");
                return Unauthorized(new { Message = "Please sign in again." });
            }

            if (!UserExists(int.Parse(userId)))
            {
                _logger.LogWarning("User in token does not exist");
                return NotFound(new { Message = "User not found. Sign in again." });
            }

            var order = await _context.Orders
                .Where(ord => ord.ID == id)
                .Include(ord => ord.Customer)
                .Include(ord => ord.Payments)
                .Include(ord => ord.OrderItems)
                .ThenInclude(orderItem => orderItem.Book)
                .ThenInclude(book => book != null ? book.Image : null)
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return NotFound(new { Message = "Order not found." });
            }

            return Ok(Order.OrderToDTO(order));
        }

        // POST api/<OrdersController>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<OrderDTO>> Post([FromBody] OrderDTO orderDTO)
        {
            _logger.LogInformation("Creating order");
            var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                _logger.LogWarning("There is no user id in token");
                return Unauthorized(new { Message = "Please sign in again." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.ID == int.Parse(userId));
            if (user == null)
            {
                _logger.LogWarning("User in token does not exist");
                return NotFound(new { Message = "User not found. Sign in again." });
            }

            bool orderExists = _context.Orders.Any(ord => ord.Total == orderDTO.Total && ord.Customer.ID == user.ID);
            if (orderExists)
            {
                _logger.LogWarning("Order exists");
                return Conflict(new { Message = "Order already exists" });
            }

            var order = new Order
            {
                CustomerID = user.ID,
                Total = orderDTO.Total,
                OrderItems = orderDTO.OrderItems
                   .Select(orderItemDTO =>
                       new OrderItem
                       {
                           Quantity = orderItemDTO.Quantity,
                           BookID = orderItemDTO.BookID
                       })
                   .ToList(),
            };

            _context.Orders.Add(order);

            try
            {
                _logger.LogInformation("Saving order to database");
                await _context.SaveChangesAsync();
                // Send Order Confirmation message
                SendOrderConfirmationEmail(user);
                return CreatedAtAction(nameof(GetOrder), new { id = order.ID }, Order.OrderToDTO(order));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return StatusCode(500, ex.Message);
            }
        }

        // PUT api/<OrdersController>/5
        [HttpPut("{id}")]
        public void PutOrder(int id, [FromBody] string value)
        {
        }

        // PATCH api/<OrdersController>/5
        [HttpPatch("{id}")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> PatchOrder(int id, [FromBody] JsonPatchDocument<Order> patchDoc)
        {
            if (patchDoc == null)
            {
                return BadRequest(ModelState);
            }

            var order = await _context.Orders
                .Where(ord => ord.ID == id)
                .Include(ord => ord.Customer)
                .Include(ord => ord.Payments)
                .FirstOrDefaultAsync();
            if (order == null)
            {
                return NotFound(new { Message = "Order not found" });
            }

            patchDoc.ApplyTo(order, ModelState);
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Update(order);
            try
            {
                await _context.SaveChangesAsync();
                return Ok(Order.OrderToDTO(order));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Error saving order to DB");
                if (!OrderExists(id))
                {
                    return NotFound(new { Message = "Order not found" });
                }
                else
                {
                    return StatusCode(500, ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving order to DB");
                return StatusCode(500, ex.Message);
            }

        }

        // DELETE api/<OrdersController>/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DeleteOrder(int id)
        {
            _logger.LogInformation("Deleting order");
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

            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                return NotFound(new { Message = "Order not found" });
            }

            _context.Orders.Remove(order);
            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        private bool OrderExists(int id) => _context.Orders.Any(u => u.ID == id);

        private bool UserExists(int id) => _context.Users.Any(u => u.ID == id);

        private void SendOrderConfirmationEmail(User receiver)
        {
            var emailData = new Email {
                    Subject = "Your order has been confirmed",
                    ToId = receiver.Email,
                    Name = receiver.Name,
                    Body = $@"<html><body>
                        <p>Hi {receiver.Name},</p>
                        <p>Thank you for making a purchase on Bookonnect! Your order has been confirmed successfully.
                           We have reached out to the owner to start delivery. You should receive the book in 3 days. If this is not the case click here.
                        </p>
                        <p>Warm regards,</p>
                        <p>Bookonnect Team.</p>"
                };

            _mailLibrary.SendMail(emailData);
        }
    }
}
