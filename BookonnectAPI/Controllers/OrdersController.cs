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
            try
            {
                int.TryParse(this.User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId);
                OrderDTO[] orders;
                if (orderQueryParameters.Total != null && orderQueryParameters.BookID != null)
                {
                    _logger.LogInformation($"Getting orders of loggedin user with Total:{orderQueryParameters.Total} and BookID: {orderQueryParameters.BookID}");
                    var bookIDSet = new HashSet<int>(orderQueryParameters.BookID);
                    orders = await _context.Orders
                        .Where(ord => ord.CustomerID == userId)
                        .Where(ord => ord.Total == orderQueryParameters.Total)
                        .Where(ord => ord.OrderItems.Where(ordItem => bookIDSet.Contains(ordItem.BookID)).Count() == orderQueryParameters.BookID.Count())
                        .Select(ord => Order.OrderToDTO(ord))
                        .ToArrayAsync();

                    return Ok(orders);
                }
                _logger.LogInformation("Getting orders of loggedin user");
                orders = await _context.Orders
                    .Where(ord => ord.CustomerID == userId)
                    .Include(ord => ord.Payments)
                    .Include(ord => ord.OrderItems)
                    .ThenInclude(orderItem => orderItem.Confirmations)
                    .Include(ord => ord.OrderItems)
                    .ThenInclude(orderItem => orderItem.Book)
                    .ThenInclude(bk => bk != null ? bk.OwnedDetails : null)
                    .ThenInclude(od => od != null ? od.Vendor : null)
                    .Select(ord => Order.OrderToDTO(ord))
                    .ToArrayAsync();

                return Ok(orders);

            } catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
           
            
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
                .ThenInclude(bk => bk != null ? bk.OwnedDetails : null)
                .ThenInclude(od => od != null ? od.Vendor : null)
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
            try
            {
                int.TryParse(this.User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId);
                /**
                 * Project bookIDs from orderDTO.OrderItems using Select
                 * Project orderItems from context and intersect with the above bookIDs. Instersect makes use of HashSet<T>
                 * so instead of O(n^2) for the check, we have O(n)
                 */
                var orderItemDTOBookIDs = orderDTO.OrderItems.Select(orderItemDTO => orderItemDTO.BookID);
                var orderItemDTOBookIDSet = new HashSet<int>(orderItemDTOBookIDs);
                bool orderExists = await _context.Orders.AnyAsync(ord =>
                    ord.Total == orderDTO.Total &&
                    ord.CustomerID == userId &&
                    ord.OrderItems.Where(orderItem => orderItemDTOBookIDSet.Contains(orderItem.BookID)).Any());

                if (orderExists)
                {
                    _logger.LogWarning("Order exists");
                    return Conflict(new { Message = "Order already exists" });
                }

                var order = new Order
                {
                    CustomerID = userId,
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
                _logger.LogInformation("Saving order to database");
                await _context.SaveChangesAsync();
                var customer = await _context.Users.FindAsync(userId);
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
        //[HttpPut("{id}")]
        //public void PutOrder(int id, [FromBody] string value)
        //{
        //}

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
            try
            {
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
                await _context.SaveChangesAsync();

                foreach (OrderItem orderItem in order.OrderItems)
                {
                    var book = await _context.Books
                        .Where(bk => bk.ID == orderItem.BookID)
                        .Include(bk => bk.OwnedDetails)
                        .ThenInclude(od => od != null ? od.Vendor : null)
                        .FirstOrDefaultAsync();

                    if (book != null && book?.OwnedDetails != null && book?.OwnedDetails?.Vendor != null)
                    {
                        // Send Dispatch message to vendor
                        SendOrderDispatchEmail(book.OwnedDetails.Vendor, order);
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
                _logger.LogError(ex, "Error deleting order");
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
            var contact = string.IsNullOrEmpty(order.Customer?.Phone) ? order.Customer?.Email : order.Customer?.Phone;
            var emailData = new Email
            {
                Subject = "Deliver the book",
                ToId = receiver.Email,
                Name = receiver.Name,
                Body = $@"<html><body>
                        <p>Hi {receiver.Name},</p>
                        <p>We've got good news! Your book has been order.</p>
                        <p>Please deliver the book using the details below:
                                To: {order.Customer?.Name}
                                Contact: {contact}
                                Location: {order.DeliveryLocation}
                                Delivery Instructions: {order.DeliveryInstructions}
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
