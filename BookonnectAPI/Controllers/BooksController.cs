using System.Security.Claims;
using BookonnectAPI.Data;
using BookonnectAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookonnectAPI.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class BooksController: ControllerBase
{
	private readonly BookonnectContext _context;
    private readonly ILogger<BooksController> _logger;
    public BooksController(BookonnectContext context, ILogger<BooksController> logger)
	{
		_context = context;
        _context.Database.EnsureCreated();
        _logger = logger;
    }

	[HttpPost]
    [Authorize]
    public async Task<ActionResult<BookDTO>> PostBook(BookDTO bookDTO)
	{
        var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return Unauthorized();
        }

        if (!UserExists(int.Parse(userId)))
        {
            return NotFound();
        }

        bool bookExists = _context.Books.Any(bk => (bk.ISBN == bookDTO.ISBN && bk.Title == bookDTO.Title && bk.Author == bookDTO.Author));
        if (bookExists)
        {
            return Conflict();
        }

        var book = new Book
        {
            Title = bookDTO.Title,
            Author = bookDTO.Author,
            ISBN = bookDTO.ISBN,
            Price = bookDTO.Price,
            Description = bookDTO.Description,
            UserID = int.Parse(userId),
        };

        _context.Books.Add(book);
        try
        {
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(PostBook), new { id = book.ID }, Book.BookToDTO(book));
        } catch (Exception)
        {
            throw;
        }
       
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookDTO>>> GetBooks([FromQuery] QueryParameter queryParameter)
    {
        var books = await _context.Books
                .OrderBy(b => b.ID)
                .Include(b => b.Image)
                .Select(b => Book.BookToDTO(b))
                .Skip(queryParameter.Size * (queryParameter.Page - 1))
                .Take(queryParameter.Size)
                .ToArrayAsync();

        return Ok(books);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<BookDTO>>> GetMyBooks()
    {
        var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            _logger.LogWarning("User id not found in token");
            return NotFound();
        }

        if (!UserExists(int.Parse(userId)))
        {
            _logger.LogWarning("User with the provided id not found");
            return NotFound();
        }

        var books = await _context.Books
                .Where(b => b.UserID == int.Parse(userId))
                .Include(b => b.Image)
                .Select(b => Book.BookToDTO(b))
                .ToArrayAsync();

        return Ok(books);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<BookDTO>> GetBook(int id)
    {
        var book = await _context.Books.Include(b => b.Image).FirstOrDefaultAsync(b => b.ID == id);

        if (book == null)
        {
            return NotFound();
        }

        return Ok(Book.BookToDTO(book));
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult> PutBook(int id, [FromQuery] BookDTO bookDTO)
    {
        if (id != bookDTO.ID)
        {
            return BadRequest();
        }

        var book = await _context.Books.FindAsync(id);
        if (book == null)
        {
            return NotFound();
        }

        book.Title = bookDTO.Title;
        book.Author = bookDTO.Author;
        book.Price = bookDTO.Price;
        book.ISBN = bookDTO.ISBN;
        book.Description = bookDTO.Description;

        _context.Entry(book).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
            return NoContent();

        }
        catch (DbUpdateConcurrencyException)
        {
            if (!BookExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }
        catch(Exception)
        {
            throw;
        }
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<BookDTO>> PatchBook(int id, [FromBody] JsonPatchDocument<Book> patchDoc)
    {
        if (patchDoc == null)
        {
            return BadRequest(ModelState);
        }

        var book = await _context.Books.FindAsync(id);
        if (book == null)
        {
            return NotFound();
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
            return new ObjectResult(Book.BookToDTO(book));
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!BookExists(id))
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

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult> DeleteBook(int id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book == null)
        {
            return NotFound();
        }

        _context.Books.Remove(book);
        try
        {
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception)
        {
            throw;
        }
    }

    private bool BookExists(int id)
    {
        return _context.Books.Any(book => book.ID == id);
    }

    private bool UserExists(int id) => _context.Users.Any(user => user.ID == id);
}

