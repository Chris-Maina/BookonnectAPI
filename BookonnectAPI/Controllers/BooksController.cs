using System.Security.Claims;
using System.Text.Json;
using BookonnectAPI.Configuration;
using BookonnectAPI.Data;
using BookonnectAPI.DTO;
using BookonnectAPI.Lib;
using BookonnectAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MySql.EntityFrameworkCore.Extensions;

namespace BookonnectAPI.Controllers;

[ApiController]
[Authorize(Policy = "UserClaimPolicy")]
[Route("/api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class BooksController: ControllerBase
{
	private readonly BookonnectContext _context;
    private readonly ILogger<BooksController> _logger;
    private readonly MailSettingsOptions _mailSettings;
    private readonly IGoogleBooksApiService _googleBooksApiService;
    private readonly IGeminiService _geminiService;
    public BooksController(BookonnectContext context, ILogger<BooksController> logger, IOptions<MailSettingsOptions> mailSettings, IGoogleBooksApiService googleBooksApiService, IGeminiService geminiService)
	{
		_context = context;
        _logger = logger;
        _mailSettings = mailSettings.Value;
        _googleBooksApiService = googleBooksApiService;
        _geminiService = geminiService;
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


        // Book is a duplicate if uploaded by the same person
        bool bookExists = await IsDuplicateBook(bookDTO, int.Parse(userId));
        if (bookExists)
        {
            return Conflict(new { Message = "Book already exists. Update it instead." });
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
                    Condition = bookDTO.Condition,
                    Quantity = bookDTO.Quantity,
                    Visible = bookDTO.Visible
                };
                // If an upload is not an affiliate then it's for local users
                if (bookDTO.AffiliateLink != null && bookDTO.AffiliateSource != null && bookDTO.AffiliateSourceID != null)
                {
                    book.AffiliateDetails = new AffiliateDetails
                    {
                        Source = bookDTO.AffiliateSource,
                        Link = bookDTO.AffiliateLink,
                        SourceID = bookDTO.AffiliateSourceID
                    };
                }
                else
                {
                    book.OwnedDetails = new OwnedDetails
                    {
                        VendorID = int.Parse(userId)
                    };
                }
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

                // Explicitly load reference navigation
                await _context.Entry(book).Reference(bk => bk.OwnedDetails).LoadAsync();
                await _context.Entry(book).Reference(bk => bk.AffiliateDetails).LoadAsync();
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
                .Include(b => b.AffiliateDetails)
                .Include(b => b.OwnedDetails)
                .ThenInclude(od => od != null ? od.Vendor : null)
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


        BookDTO[]? books;
        var isBookonnectAdmin = await _context.Users.AnyAsync(u => u.ID == int.Parse(userId) && u.Email == _mailSettings.EmailId);
        if (isBookonnectAdmin)
        {
            // include affiliate books
            books = await _context.Books
                .Where(b => b.OwnedDetails != null && b.OwnedDetails.VendorID == int.Parse(userId) || b.AffiliateDetails != null)
                .Include(b => b.Image)
                .Include(b => b.AffiliateDetails)
                .Include(b => b.OwnedDetails)
                .ThenInclude(od => od != null ? od.Vendor : null)
                .Select(b => Book.BookToDTO(b))
                .ToArrayAsync();

            return Ok(books);
        }

        books = await _context.Books
            .Where(b => b.OwnedDetails != null && b.OwnedDetails.VendorID == int.Parse(userId))
            .Include(b => b.Image)
            .Include(b => b.OwnedDetails)
            .ThenInclude(od =>  od != null ? od.Vendor : null)
            .Select(b => Book.BookToDTO(b))
            .ToArrayAsync();

        return Ok(books);
    }

    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BookSearchDTO>>> SearchBook([FromQuery] SearchQueryParameters queryParameters)
    {
        _logger.LogInformation("Searching book");

        if (string.IsNullOrEmpty(queryParameters.SearchTerm))
        {
            
            return Ok();
        }

        var results = await _context.Books
            .Where(bk => EF.Functions.Like(bk.Title, $"%{queryParameters.SearchTerm}%") || EF.Functions.Like(bk.Author, $"%{queryParameters.SearchTerm}%"))
            .Include(bk => bk.Image)
            .Select(bk => Book.BookToSearchDTO(bk))
            .ToArrayAsync();

        if (results != null)
        {
            _logger.LogInformation("Found book in our DB");
            return Ok(results);
        }

        try
        {
            var response = await _googleBooksApiService.SearchBook(queryParameters.SearchTerm);
            _logger.LogInformation("Found book from Google Books API");
            var result = GoogleBooksApiService.ConvertResponseToSearchDTO(response);

            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"A HttpRequestException occurred: {ex.Message}", ex);
            return StatusCode(ex.StatusCode != null ? (int)ex.StatusCode : 500, ex.Message);
        }
        catch (JsonException ex)
        {
            _logger.LogError($"A JsonException occurred: {ex.Message}", ex);
            return StatusCode(500, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError($"An Exception occurred: {ex.Message}", ex);
            return StatusCode(500, ex.Message);
        }   

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
            .Include(b => b.AffiliateDetails)
            .Include(b => b.OwnedDetails)
            .ThenInclude(od => od != null ? od.Vendor : null)
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
            .Include(bk => bk.Image)
            .Include(b => b.AffiliateDetails)
            .Include(b => b.OwnedDetails)
            .ThenInclude(od => od != null ? od.Vendor : null)
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
                // Change stock quantity to 0 and visibility to false
                book.Quantity = 0;
                book.Visible = false;
                _context.Entry(book).State = EntityState.Modified;
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

    private async Task<bool> IsDuplicateBook(BookDTO bookDTO, int vendorID)
    {
        if (await _context.Books.AnyAsync(bk => (
            bk.ISBN == bookDTO.ISBN &&
            bk.Title == bookDTO.Title &&
            bk.Author == bookDTO.Author
        )))
        {
            if (bookDTO.AffiliateLink != null && bookDTO.AffiliateSource != null)
            {
                return await _context.AffiliateDetails.AnyAsync(ad => ad.Link == bookDTO.AffiliateLink && ad.Source == bookDTO.AffiliateSource);
            }
            else
            {
                return await _context.OwnedDetails.AnyAsync(od => od.VendorID == vendorID);
            }    

        }
        else
        {
            return false; // title, ISBN and author combination is unique, therefore not a duplicate.
        }
    }
}

