using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookonnectAPI.Data;
using BookonnectAPI.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.JsonPatch;

namespace BookonnectAPI.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = "UserClaimPolicy")]
    [ApiController]
    [ApiVersion("1.0")]
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
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<CartItemDTO>>> GetCartItems()
        {
            var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                _logger.LogWarning("User id not found in token");
                return Unauthorized(new { Message = "Please sign in again." });
            }

            _logger.LogInformation("Fetching cart items");
            var cartItems = await _context.CartItems
                .Where(c => c.UserID == int.Parse(userId))
                .Include(c => c.Book)
                .ThenInclude(b => b != null ? b.Image : null)
                .Include(c => c.Book)
                .ThenInclude(b => b != null ? b.Vendor : null)
                .Select(c => CartItem.CartItemToDTO(c))
                .ToArrayAsync();

            return Ok(cartItems);
        }

        // GET: api/CartItems/5
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<CartItemDTO>> GetCartItem(int id)
        {
            var cartItem = await _context.CartItems.FindAsync(id);

            if (cartItem == null)
            {
                return NotFound(new { Message = "Cart item not found." });
            }

            return Ok(CartItem.CartItemToDTO(cartItem));
        }

        // PUT: api/CartItems/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutCartItem(int id, CartItemDTO cartItemDTO)
        {
            if (id != cartItemDTO.ID)
            {
                _logger.LogWarning("ID in params {0} does not match cart item id {1}", id, cartItemDTO.ID);
                return BadRequest(new { message = "Wrong book id in URL. Check and try again." });
            }

            var cartItem = await _context.CartItems
                .Where(ci => ci.ID == id)
                .Include(ci => ci.Book)
                .FirstOrDefaultAsync();

            if (cartItem == null)
            {
                return NotFound(new { Message = "Cart item not found." });
            }

            // check if quantity is greater than stock quantity
            if (cartItemDTO.Quantity > cartItem.Book?.Quantity)
            {
                return BadRequest(new { Message = "Cannot order more than what is in stock" });
            }

            _context.Entry(cartItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!CartItemExists(id))
                {
                    return NotFound(new { Message = "Cart item not found." });
                }
                else
                {
                    return StatusCode(500, ex.Message);
                }
            }
        }

        [HttpPatch("{id}")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CartItemDTO>> PatchCartItem(int id, [FromBody] JsonPatchDocument<CartItem> patchDoc)
        {
            if (patchDoc == null)
            {
                return BadRequest(ModelState);
            }

            var cartItem = await _context.CartItems
                .Where(ci => ci.ID == id)
                .Include(ci => ci.Book)
                .FirstOrDefaultAsync();

            if (cartItem == null)
            {
                return NotFound(new { Message = "Cart item not found." });
            }

            // check if quantity is greater than stock quantity
            if (cartItem.Quantity > cartItem.Book?.Quantity)
            {
                return BadRequest(new { Message = "Cannot order more than what is in stock" });
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
                return Ok(CartItem.CartItemToDTO(cartItem));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!CartItemExists(id))
                {
                    return NotFound(new { Message = "Cart item not found." });
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

        // POST: api/CartItems
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CartItemDTO>> PostCartItem(CartItemDTO cartItemDTO)
        {
            var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                _logger.LogWarning("User id not found in token");
                return Unauthorized(new { Message = "Please sign in again." });
            }

            bool cartItemExist = _context.CartItems.Any(cartItem => cartItem.BookID == cartItemDTO.BookID && cartItem.UserID == int.Parse(userId));
            if (cartItemExist)
            {
                _logger.LogWarning("A cart item with the associated book exists");
                return Conflict(new { Message = "A cart item with the associated book exists" });
            }

            bool myBookExistsInCart = _context.Books.Any(book =>
                book.VendorID == int.Parse(userId) &&
                book.ID == cartItemDTO.BookID);

            if (myBookExistsInCart)
            {
                _logger.LogWarning("Added a book you own in cart");
                return BadRequest(new { Message = "You cannnot buy your own book." });
            }

            bool isQuantityMoreThanStockQuantity = _context.Books.Any(book =>
                book.ID == cartItemDTO.BookID &&
                cartItemDTO.Quantity > book.Quantity);

            if (isQuantityMoreThanStockQuantity)
            {
                _logger.LogWarning("Cannot order more than what is in stock");
                return BadRequest(new { Message = "You cannnot order more than what is in stock." });
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
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }

        }

        // DELETE: api/CartItems/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DeleteCartItem(int id)
        {
            _logger.LogInformation("Fetching item to delete");
            var cartItem = await _context.CartItems.FindAsync(id);
            if (cartItem == null)
            {
                return NotFound(new { Message = "Cart item not found." });
            }

            _context.CartItems.Remove(cartItem);

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            } catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }

        }

        // POST: api/CartItems/Delete
        [HttpPost]
        [Route("Delete")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DeleteMultipleCartItems([FromQuery] int[] id)
        {
            var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                _logger.LogWarning("User id not found in token");
                return Unauthorized(new { Message = "Please sign in again." });
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
                        _logger.LogWarning("Could not find cart item with id {0}", cartItemId);
                        return NotFound(new { Message = "Could not find one of the cart items" });
                    }
                    _context.CartItems.Remove(cartItem);
                }

                await _context.SaveChangesAsync();
                _logger.LogWarning("Successfully deleted cart items");
                return NoContent();
            } catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }
        private bool CartItemExists(int id)
        {
            return _context.CartItems.Any(e => e.ID == id);
        }
    }
}
