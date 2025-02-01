﻿using System.Security.Claims;
using BookonnectAPI.Data;
using BookonnectAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookonnectAPI.Controllers;

[ApiController]
[Authorize(Policy = "UserClaimPolicy")]
[Route("/api/[controller]")]
public class BooksController: ControllerBase
{
	private readonly BookonnectContext _context;
    private readonly ILogger<BooksController> _logger;
    public BooksController(BookonnectContext context, ILogger<BooksController> logger)
	{
		_context = context;
        _logger = logger;
    }

	[HttpPost]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BookDTO>> PostBook(BookDTO bookDTO)
	{
        _logger.LogInformation("Posting a book");
        var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized(new { Message = "Please sign in again." });
        }

        bool bookExists = _context.Books.Any(bk => (bk.ISBN == bookDTO.ISBN && bk.Title == bookDTO.Title && bk.Author == bookDTO.Author));
        if (bookExists)
        {
            return Conflict(new { Message = "Book already exists." });
        }

        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                var book = new Book
                {
                    Title = bookDTO.Title,
                    Author = bookDTO.Author,
                    ISBN = bookDTO.ISBN,
                    Price = bookDTO.Price,
                    Description = bookDTO.Description,
                    VendorID = int.Parse(userId),
                    Condition = bookDTO.Condition,
                    Quantity = bookDTO.Quantity,
                };
                _context.Books.Add(book);
                await _context.SaveChangesAsync();

                var inventoryLog = new InventoryLog
                {
                    BookID = book.ID,
                    Quantity = bookDTO.Quantity,
                    Type = ChangeType.InitialStock,
                    DateTime = DateTime.Now
                };
                _context.InventoryLogs.Add(inventoryLog);
                await _context.SaveChangesAsync();

                // Commit transaction if all operations succeed
                await transaction.CommitAsync();

                // Explicitly loading Vendor reference navigation
                await _context.Entry(book).Reference(bk => bk.Vendor).LoadAsync();
                return CreatedAtAction(nameof(PostBook), new { id = book.ID }, Book.BookToDTO(book));

            } catch (Exception ex)
            {
                // Rollback transaction if any operation fails
                await transaction.RollbackAsync();
                return StatusCode(500, ex.Message);
            }

        }
       
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BookDTO>>> GetBooks([FromQuery] QueryParameter queryParameter)
    {
        _logger.LogInformation("Getting books");
        var books = await _context.Books
                .Where(b => b.Visible == true && b.Quantity > 0)
                .OrderBy(b => b.ID)
                .Include(b => b.Image)
                .Include(b => b.Vendor)
                .Select(b => Book.BookToDTO(b))
                .Skip(queryParameter.Size * (queryParameter.Page - 1))
                .Take(queryParameter.Size)
                .ToArrayAsync();

        return Ok(books);
    }

    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BookDTO>>> GetMyBooks()
    {
        _logger.LogInformation("Getting my books");
        var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            _logger.LogWarning("User id not found in token");
            return Unauthorized(new { Message = "Please sign in again." });
        }

        var books = await _context.Books
                .Where(b => b.VendorID == int.Parse(userId))
                .Include(b => b.Image)
                .Include(b => b.Vendor)
                .Select(b => Book.BookToDTO(b))
                .ToArrayAsync();

        return Ok(books);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<BookDTO>> GetBook(int id)
    {
        _logger.LogInformation("Getting book with id {0}", id);
        var book = await _context.Books
            .Where(b => b.ID == id)
            .Include(b => b.Image)
            .Include(b => b.Vendor)
            .FirstOrDefaultAsync();

        if (book == null)
        {
            return NotFound(new { Message = "Book not found." });
        }

        return Ok(Book.BookToDTO(book));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> PutBook(int id, [FromQuery] BookDTO bookDTO)
    {
        _logger.LogInformation($"Updating book with id {id}");
        if (id != bookDTO.ID)
        {
            return BadRequest(new { Message = "Provided book id does not match. Check and try again" });
        }

        var book = await _context.Books.FindAsync(id);

        if (book == null)
        {
            return NotFound(new { Message = "Book not found." });
        }

        book.Title = bookDTO.Title;
        book.Author = bookDTO.Author;
        book.Price = bookDTO.Price;
        book.ISBN = bookDTO.ISBN;
        book.Visible = bookDTO.Visible;
        book.Condition = bookDTO.Condition;
        book.Quantity = bookDTO.Quantity;
        if (bookDTO.Description != null)
        {
            book.Description = bookDTO.Description;
        }

        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                _context.Entry(book).State = EntityState.Modified;
                await _context.SaveChangesAsync();


                // Get difference and create a new Inventory Log i.e an addition/subtraction
                int quantityAdjustment = bookDTO.Quantity - book.Quantity;
                if (quantityAdjustment != 0)
                {
                    var inventoryLog = new InventoryLog
                    {
                        BookID = book.ID,
                        Quantity = bookDTO.Quantity - book.Quantity,
                        Type = ChangeType.Adjustment,
                        DateTime = DateTime.Now
                    };
                    _context.InventoryLogs.Add(inventoryLog);
                    await _context.SaveChangesAsync();
                }

                // Commit transaction
                await transaction.CommitAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Rollback
                await transaction.RollbackAsync();
                if (!BookExists(id))
                {
                    return NotFound(new { Message = "Book not found." });
                }
                else
                {
                    return Conflict();
                }
            }
            catch (Exception ex)
            {
                // Rollback
                await transaction.RollbackAsync();
                return StatusCode(500, ex.Message);
            }
        }
    }

    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BookDTO>> PatchBook(int id, [FromBody] JsonPatchDocument<Book> patchDoc)
    {
        _logger.LogInformation($"Patching book with id {id}");
        if (patchDoc == null)
        {
            return BadRequest(ModelState);
        }

        // Eager loading references
        var book = await _context.Books
            .Where(b => b.ID == id)
            .Include(b => b.Vendor)
            .Include(bk => bk.Image)
            .FirstOrDefaultAsync();

        if (book == null)
        {
            return NotFound(new { Message = "Book not found." });
        }

        // check if quantity is in the patch document
        var quantityOperation = patchDoc.Operations.FirstOrDefault(op => op.path.Equals("/quantity", StringComparison.OrdinalIgnoreCase));
        if (quantityOperation != null)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Get difference and create a new Inventory Log i.e an addition/subtraction
                    int newQuantity = Convert.ToInt32(quantityOperation.value);
                    var inventoryLog = new InventoryLog
                    {
                        BookID = id,
                        Quantity = newQuantity - book.Quantity,
                        Type = ChangeType.Adjustment,
                        DateTime = DateTime.Now
                    };
                    _context.InventoryLogs.Add(inventoryLog);
                    await _context.SaveChangesAsync();

                    patchDoc.ApplyTo(book, ModelState);
                    if (!ModelState.IsValid)
                    {
                        return BadRequest(ModelState);
                    }

                    _context.Update(book);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return Ok(Book.BookToDTO(book));
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Rollback
                    await transaction.RollbackAsync();
                    if (!BookExists(id))
                    {
                        return NotFound(new { Message = "Book not found." });
                    }
                    else
                    {
                        return Conflict();
                    }
                }
                catch (Exception ex)
                {
                    // Rollback
                    await transaction.RollbackAsync();
                    return StatusCode(500, ex.Message);
                }
            }
        }

        patchDoc.ApplyTo(book, ModelState);
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _context.Update(book);
        try
        {
            await _context.SaveChangesAsync();
            return Ok(Book.BookToDTO(book));
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!BookExists(id))
            {
                return NotFound(new { Message = "Book not found." });
            }
            else
            {
                return Conflict();
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteBook(int id)
    {
        _logger.LogInformation($"Deleting book with id {id}");
        var book = await _context.Books.FindAsync(id);
        if (book == null)
        {
            return NotFound(new { Message = "Book not found." });
        }

        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();

                // Update change quantity to -currentQuantity
                var inventoryLog = new InventoryLog
                {
                    BookID = book.ID,
                    DateTime = DateTime.Now,
                    Type = ChangeType.Deletion,
                    Quantity = -book.Quantity
                };

                _context.InventoryLogs.Add(inventoryLog);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, ex.Message);
            }
        }   
    }

    private bool BookExists(int id)
    {
        return _context.Books.Any(book => book.ID == id);
    }
}

