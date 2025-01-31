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

        var book = new Book
        {
            Title = bookDTO.Title,
            Author = bookDTO.Author,
            ISBN = bookDTO.ISBN,
            Price = bookDTO.Price,
            Description = bookDTO.Description,
            VendorID = int.Parse(userId),
            Condition = bookDTO.Condition,
        };

        _context.Books.Add(book);
        try
        {
            await _context.SaveChangesAsync();
            // Explicitly loading Vendor reference navigation
            await _context.Entry(book).Reference(bk => bk.Vendor).LoadAsync();
            return CreatedAtAction(nameof(PostBook), new { id = book.ID }, Book.BookToDTO(book));
        } catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
       
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BookDTO>>> GetBooks([FromQuery] QueryParameter queryParameter)
    {
        var books = await _context.Books
                .Where(b => b.Visible == true)
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
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> PutBook(int id, [FromQuery] BookDTO bookDTO)
    {
        if (id != bookDTO.ID)
        {
            return BadRequest(new { Message = "Provided book id does not match. Check and try again" });
        }

        // Eager loading
        var book = await _context.Books
            .Where(b => b.ID == id)
            .Include(b => b.Vendor)
            .Include(bk => bk.Image)
            .FirstOrDefaultAsync();

        if (book == null)
        {
            return NotFound(new { Message = "Book not found." });
        }

        book.Title = bookDTO.Title;
        book.Author = bookDTO.Author;
        book.Price = bookDTO.Price;
        book.ISBN = bookDTO.ISBN;
        book.Description = bookDTO.Description;
        book.Visible = bookDTO.Visible;
        book.Condition = bookDTO.Condition;

        _context.Entry(book).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
            return NoContent();

        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (!BookExists(id))
            {
                return NotFound(new { Message = "Book not found." });
            }
            else
            {
                return StatusCode(500, ex.Message);
            }
        }
        catch(Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BookDTO>> PatchBook(int id, [FromBody] JsonPatchDocument<Book> patchDoc)
    {
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
        catch (DbUpdateConcurrencyException ex)
        {
            if (!BookExists(id))
            {
                return NotFound(new { Message = "Book not found." });
            }
            else
            {
                return StatusCode(500, ex.Message);
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
        var book = await _context.Books.FindAsync(id);
        if (book == null)
        {
            return NotFound(new { Message = "Book not found." });
        }

        _context.Books.Remove(book);
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

    private bool BookExists(int id)
    {
        return _context.Books.Any(book => book.ID == id);
    }
}

