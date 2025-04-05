using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookonnectAPI.Data;
using BookonnectAPI.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BookonnectAPI.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Policy = "UserClaimPolicy")]
[ApiController]
[ApiVersion("1.0")]
public class ReviewsController : ControllerBase
{
    private readonly BookonnectContext _context;
    private readonly ILogger<Review> _logger;

    public ReviewsController(BookonnectContext context, ILogger<Review> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/Reviews
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReviewDTO>>> GetReviews([FromQuery] QueryParameter queryParameter)
    {
        _logger.LogInformation("Getting reviews");
        try
        {
            int.TryParse(this.User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId);
            var rewiews = await _context.Reviews
            .Where(rev => rev.UserID == userId)
            .OrderByDescending(rev => rev.DateTime)
            .Include(rev => rev.Book)
            .ThenInclude(bk => bk != null ? bk.Image : null)
            .Select(rev => Review.ReviewToDTO(rev))
            .Skip(queryParameter.Size * (queryParameter.Page - 1))
            .Take(queryParameter.Size)
            .ToArrayAsync();

            return Ok(rewiews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return StatusCode(500, ex.Message);
        }
    }

    // GET: api/Reviews/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ReviewDTO>> GetReview(int id)
    {
        var review = await _context.Reviews.FindAsync(id);

        if (review == null)
        {
            return NotFound();
        }

        return Ok(Review.ReviewToDTO(review));
    }

    // PUT: api/Reviews/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutReview(int id, Review review)
    {
        if (id != review.ID)
        {
            return BadRequest();
        }

        _context.Entry(review).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ReviewExists(id))
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

    // POST: api/Reviews
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<ReviewDTO>> PostReview(ReviewDTO reviewDTO)
    {
        _logger.LogInformation("Posting review");
        try
        {
            int.TryParse(this.User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId);
            bool reviewExists = await _context.Reviews.AnyAsync(rev => rev.UserID == userId && rev.BookID == reviewDTO.BookID && (rev.Text == reviewDTO.Text || rev.Status == reviewDTO.Status));
            if (reviewExists)
            {
                _logger.LogInformation("Found an existing review");
                return Conflict(new { Message = "Review exists." });
            }

            var review = new Review
            {
                BookID = reviewDTO.BookID,
                UserID = userId,
                Text = reviewDTO.Text,
                Status = reviewDTO.Status,
                DateTime = DateTime.Now
            };
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetReview", new { id = review.ID }, Review.ReviewToDTO(review));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return StatusCode(500, ex.Message);
        }
    }

    // DELETE: api/Reviews/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReview(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
        {
            return NotFound();
        }

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ReviewExists(int id)
    {
        return _context.Reviews.Any(e => e.ID == id);
    }
}
