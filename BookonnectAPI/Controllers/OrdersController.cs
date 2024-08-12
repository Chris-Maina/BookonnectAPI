using System.Security.Claims;
using BookonnectAPI.Data;
using BookonnectAPI.Models;
using Microsoft.AspNetCore.Authorization;
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
        public OrdersController(BookonnectContext context, ILogger<OrdersController> logger)
        {
            _context = context;
            context.Database.EnsureCreated();
            _logger = logger;
        }

        // GET: api/<OrdersController>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> Get()
        {
            _logger.LogInformation("Getting orders");
            var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                _logger.LogWarning("No token found");
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                _logger.LogWarning("User in token not found");
                return NotFound();
            }

            _logger.LogInformation("Fetching orders by logged in user");
            var orders = _context.Orders
                .Where(ord => ord.UserID == user.ID)
                .Include(ord => ord.Delivery)
                .Include(ord => ord.OrderItems)
                .ThenInclude(orderItem => orderItem.Book)
                //.ThenInclude(book => book.Image)
                .Select(ord => Order.OrderToDTO(ord));

            return Ok(await orders.ToArrayAsync());
        }

        // GET api/<OrdersController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<OrdersController>
        [HttpPost]
        public async Task<ActionResult<OrderDTO>> Post([FromBody] OrderDTO orderDTO)
        {
            _logger.LogInformation("Creating order");
            var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                _logger.LogWarning("There is no user id in token");
                return Unauthorized();
            }

            if (!UserExists(int.Parse(userId)))
            {
                _logger.LogWarning("User in token does not exist");
                return NotFound();
            }

            bool orderExists = _context.Orders.Any(ord => ord.Total == orderDTO.Total && ord.Status == orderDTO.Status && ord.User.ID == int.Parse(userId));
            if (orderExists)
            {
                _logger.LogWarning("Order exists");
                return Conflict();
            }

            var order = new Order
            {
                UserID = int.Parse(userId),
                Status = orderDTO.Status,
                Total = orderDTO.Total,
                DeliveryID = orderDTO.DeliveryID,
                PaymentID = orderDTO.PaymentID,
                OrderItems = orderDTO.OrderItems
                    .Select(orderItemDTO =>
                        new OrderItem {
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
                return CreatedAtAction(nameof(Post), new { id = order.ID }, Order.OrderToDTO(order));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                throw;
            }

        }

        // PUT api/<OrdersController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<OrdersController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        private bool UserExists(int id) => _context.Users.Any(u => u.ID == id);
    }
}
