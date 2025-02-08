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
    [Route("/api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize(Policy = "UserClaimPolicy")]
    [ApiVersion("1.0")]
    public class OrdersController : ControllerBase
    {
        private readonly BookonnectContext _context;
        private readonly ILogger<OrdersController> _logger;
        private readonly IMailLibrary _mailLibrary;
        public OrdersController(BookonnectContext context, ILogger<OrdersController> logger, IMailLibrary mailLibrary)
        {
            _context = context;
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
           
            OrderDTO[] orders;
            if (orderQueryParameters.Total != null && orderQueryParameters.BookID != null)
            {
                _logger.LogInformation("Fetching orders by logged in user and query params Total and BookID");
                var bookIDSet = new HashSet<int>(orderQueryParameters.BookID);
                orders = await _context.Orders
                    .Where(ord => ord.CustomerID == int.Parse(userId))
                    .Where(ord => ord.Total == orderQueryParameters.Total)
                    .Where(ord => ord.OrderItems.Where(ordItem => bookIDSet.Contains(ordItem.BookID)).Count() == orderQueryParameters.BookID.Count())
                    .Select(ord => Order.OrderToDTO(ord))
                    .ToArrayAsync();

                return Ok(orders);
            }
            _logger.LogInformation("Fetching orders by logged in user");
            orders = await _context.Orders
                .Where(ord => ord.CustomerID == int.Parse(userId))
                .Include(ord => ord.Payments)
                .Include(ord => ord.OrderItems)
                .ThenInclude(orderItem => orderItem.Confirmations)
                .Include(ord => ord.OrderItems)
                .ThenInclude(orderItem => orderItem.Book)
                .ThenInclude(bk => bk != null ? bk.Vendor : null)
                .Select(ord => Order.OrderToDTO(ord))
                .ToArrayAsync();

            return Ok(orders);
        }

        // GET api/<OrdersController>/5
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<OrderDTO>> GetOrder(int id)
        {
            _logger.LogInformation("Getting order");
            var order = await _context.Orders
                .Where(ord => ord.ID == id)
                .Include(ord => ord.Customer)
                .Include(ord => ord.Payments)
                .Include(ord => ord.OrderItems)
                .ThenInclude(orderItem => orderItem.Book)
                .ThenInclude(book => book != null ? book.Image : null)
                .Include(ord => ord.OrderItems)
                .ThenInclude(orderItem => orderItem.Book)
                .ThenInclude(book => book != null ? book.Vendor : null)
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

            /**
             * Project bookIDs from orderDTO.OrderItems using Select
             * Project orderItems from context and intersect with the above bookIDs. Instersect makes use of HashSet<T>
             * so instead of O(n^2) for the check, we have O(n)
             */
            var orderItemDTOBookIDs = orderDTO.OrderItems.Select(orderItemDTO => orderItemDTO.BookID);
            var orderItemDTOBookIDSet = new HashSet<int>(orderItemDTOBookIDs);
            bool orderExists = _context.Orders.Any(ord =>
                ord.Total == orderDTO.Total &&
                ord.CustomerID == int.Parse(userId) &&
                ord.OrderItems.Where(orderItem => orderItemDTOBookIDSet.Contains(orderItem.BookID)).Any());

            if (orderExists)
            {
                _logger.LogWarning("Order exists");
                return Conflict(new { Message = "Order already exists" });
            }

            var order = new Order
            {
                CustomerID = int.Parse(userId),
                Total = orderDTO.Total,
                DateTime = DateTime.Now,
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
                var customer = await _context.Users.FindAsync(int.Parse(userId));
                if (customer != null)
                {
                    // Send Order Confirmation message to customer
                    SendOrderConfirmationEmail(customer);
                }

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
            _logger.LogInformation("Updating order with delivery location and instructions");
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

                foreach (OrderItem orderItem in order.OrderItems)
                {
                    var book = await _context.Books
                        .Where(bk => bk.ID == orderItem.BookID)
                        .Include(bk => bk.Vendor)
                        .FirstOrDefaultAsync();
                    if (book != null)
                    {
                        // Send Dispatch message to vendor
                        SendOrderDispatchEmail(book.Vendor, order);
                    }
                }
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

        private void SendOrderConfirmationEmail(User receiver)
        {
            var emailData = new Email {
                    Subject = "Your order has been confirmed",
                    ToId = receiver.Email,
                    Name = receiver.Name,
                    Body = $@"<html><body>
                        <p>Hi {receiver.Name},</p>
                        <p>Thank you for making a purchase on Bookonnect! Your order has been confirmed successfully.
                           You can track the status of the order under My Orders.
                        </p>
                        <p>Warm regards,</p>
                        <p>Bookonnect Team.</p>"
                };

            _logger.LogInformation($"Sending confimation email to {receiver.Name}");
            _mailLibrary.SendMail(emailData);
        }

        private void SendOrderDispatchEmail(User receiver, Order order)
        {
            var emailData = new Email
            {
                Subject = "Deliver the book",
                ToId = receiver.Email,
                Name = receiver.Name,
                Body = $@"<html><body>
                        <p>Hi {receiver.Name},</p>
                        <p>We've got good news! Your book has been order.</p>
                        <p>Please deliver the book using details:
                                Location: {order.DeliveryLocation}
                                Instructions: {order.DeliveryInstructions}
                        </p>
                        <p> Please update the status upon dispatch in your Profile.</p>

                        <p>Warm regards,</p>
                        <p>Bookonnect Team.</p>"
            };

            _logger.LogInformation($"Sending dispatch email to {receiver.Name}");
            _mailLibrary.SendMail(emailData);
        }
    }
}
