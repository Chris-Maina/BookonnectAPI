using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookonnectAPI.Data;
using BookonnectAPI.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging;

namespace BookonnectAPI.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class CartItemsController : ControllerBase
    {
        private readonly BookonnectContext _context;
        private readonly ILogger<CartItem> _logger;

        public CartItemsController(BookonnectContext context, ILogger<CartItem> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/CartItems
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CartItemDTO>>> GetCartItems()
        {
            var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                _logger.LogWarning("User id not found in token");
                return Unauthorized();
            }

            if (!UserExists(int.Parse(userId)))
            {
                return NotFound();
            }

            _logger.LogInformation("Fetching cart items");
            var cartItems = _context.CartItems
                .Where(c => c.UserID == int.Parse(userId))
                .Include(c => c.Book)
                .ThenInclude(b => b != null ? b.Image : null)
                .Select(c => CartItem.CartItemToDTO(c));

            return await cartItems.ToListAsync();
        }

        // GET: api/CartItems/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CartItemDTO>> GetCartItem(int id)
        {
            var cartItem = await _context.CartItems.FindAsync(id);

            if (cartItem == null)
            {
                return NotFound();
            }

            return CartItem.CartItemToDTO(cartItem);
        }

        // PUT: api/CartItems/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCartItem(int id, CartItem cartItem)
        {
            if (id != cartItem.ID)
            {
                return BadRequest();
            }

            _context.Entry(cartItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CartItemExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult<CartItemDTO>> PatchCartItem(int id, [FromBody] JsonPatchDocument<CartItem> patchDoc)
        {
            if (patchDoc == null)
            {
                return BadRequest(ModelState);
            }

            var cartItem = await _context.CartItems.FindAsync(id);
            if (cartItem == null)
            {
                return NotFound();
            }

            patchDoc.ApplyTo(cartItem, ModelState);
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Update(cartItem);
            try
            {
                await _context.SaveChangesAsync();
                return new ObjectResult(CartItem.CartItemToDTO(cartItem));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CartItemExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        // POST: api/CartItems
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<CartItemDTO>> PostCartItem(CartItemDTO cartItemDTO)
        {
            var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                _logger.LogWarning("User id not found in token");
                return Unauthorized();
            }

            if (!UserExists(int.Parse(userId)))
            {
                return NotFound();
            }

            bool cartItemExist = _context.CartItems.Any(cartItem => cartItem.BookID == cartItemDTO.BookID && cartItem.UserID == int.Parse(userId));
            if (cartItemExist)
            {
                _logger.LogWarning("A cart item with the associated book exists");
                return Conflict();
            }

            var cartItem = new CartItem
            {
                UserID = int.Parse(userId),
                Quantity = cartItemDTO.Quantity,
                BookID = cartItemDTO.BookID,
            };

            _context.CartItems.Add(cartItem);
            try
            {
                await _context.SaveChangesAsync();
                return CreatedAtAction("GetCartItem", new { id = cartItem.ID }, CartItem.CartItemToDTO(cartItem));
            }
            catch (Exception)
            {
                throw;
            }

        }

        // DELETE: api/CartItems/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCartItem(int id)
        {
            _logger.LogInformation("Fetching item to delete");
            var cartItem = await _context.CartItems.FindAsync(id);
            if (cartItem == null)
            {
                return NotFound();
            }

            _context.CartItems.Remove(cartItem);

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            } catch (Exception)
            {
                throw;
            }

        }

        // POST: api/CartItems/Delete
        [HttpPost]
        [Route("Delete")]
        public async Task<ActionResult> DeleteMultipleCartItems([FromQuery] int[] id)
        {
            var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                _logger.LogWarning("User id not found in token");
                return Unauthorized();
            }

            if (!UserExists(int.Parse(userId)))
            {
                _logger.LogWarning("User with id {0} not found", userId);
                return NotFound();
            }

            try
            {
                // retreaving the cart items to be deleted
                _logger.LogInformation("Fetching items to delete");
                foreach (int cartItemId in id)
                {
                    var cartItem = await _context.CartItems.FirstOrDefaultAsync(ci => ci.ID == cartItemId && ci.UserID == int.Parse(userId));
                    if (cartItem == null)
                    {
                        _logger.LogWarning("Could not find cart item with id {0}", id);
                        return NotFound();
                    }
                    _context.CartItems.Remove(cartItem);
                }

                await _context.SaveChangesAsync();
                _logger.LogWarning("Successfully deleted cart items");
                return NoContent();
            } catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500);
            }
        }
        private bool CartItemExists(int id)
        {
            return _context.CartItems.Any(e => e.ID == id);
        }

        private bool UserExists(int id) => _context.Users.Any(user => user.ID == id);
    }
}
