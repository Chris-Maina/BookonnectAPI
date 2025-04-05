using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookonnectAPI.Data;
using BookonnectAPI.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BookonnectAPI.Lib;
using System.Text.Json;
using BookonnectAPI.DTO;

namespace BookonnectAPI.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Policy = "UserClaimPolicy")]
[ApiController]
[ApiVersion("1.0")]
public class RecommendationsController : ControllerBase
{
    private readonly BookonnectContext _context;
    private readonly ILogger<Recommendation> _logger;
    private readonly IGeminiService _geminiService;

    public RecommendationsController(BookonnectContext context, ILogger<Recommendation> logger, IGeminiService geminiService)
    {
        _context = context;
        _logger = logger;
        _geminiService = geminiService;
    }

    // GET: api/Recommendations
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RecommendationDTO>>> GetRecommendation()
    {
        _logger.LogInformation("Fetching recommendations");
        var recommendations = await _context.Recommendations
            .OrderByDescending(rec => rec.ID)
            .Include(rec => rec.Book)
            .Select(rec => Recommendation.RecommendationToDTO(rec))
            .ToListAsync();

        return Ok(recommendations);
    }

    // GET: api/Recommendations/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Recommendation>> GetRecommendation(int id)
    {
        var recommendation = await _context.Recommendations.FindAsync(id);

        if (recommendation == null)
        {
            return NotFound();
        }

        return recommendation;
    }

    // PUT: api/Recommendations/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutRecommendation(int id, Recommendation recommendation)
    {
        if (id != recommendation.ID)
        {
            return BadRequest();
        }

        _context.Entry(recommendation).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!RecommendationExists(id))
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

    // POST: api/Recommendations
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateRecommendations()
    {
        _logger.LogInformation("Generating book recommendations");
        try
        {
            int.TryParse(this.User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId);
            Review[]? reviews = await _context.Reviews
                .Where(review => review.UserID == userId)
                .Include(review => review.Book)
                .Include(review => review.User)
                .ToArrayAsync();

            if (reviews == null || reviews.Length == 0)
            {
                _logger.LogInformation("User does not have reviews");
                return NotFound(new { Message = "Please submit at least 5 book reviews" });
            }

            var review = reviews.FirstOrDefault(review => review.User?.Email != null);
            if (review == null)
            {
                _logger.LogError("Reviews are not associated to a user");
                return NotFound(new { Message = "Could not get your recommendations. Update your reviews." });
            }

            var prompt = _geminiService.GetRecommendationsPrompt(review.User?.Email!, reviews);
            GeminiApiResponse? response = await _geminiService.GenerateContent(prompt);
            BookSearchDTO[]? booksInResponse = _geminiService.DeserializeGeminiResponse(response);

            if (booksInResponse == null)
            {
                _logger.LogInformation("Deserialization returned a null value");
                return NotFound(new { Message = "Could not get your recommendations. Update your reviews." });
            }

            _logger.LogInformation("Saving LLM response to DB");
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // save books
                    var books = new List<Book>();
                    List<string?> titlesToCheck = booksInResponse.Select(bk => bk.Title).ToList();
                    List<Book> existingBooks = await _context.Books
                        .Where(bk => titlesToCheck.Contains(bk.Title))
                        .ToListAsync();
                    foreach(var bkInResp in booksInResponse)
                    {
                        var existingBook = existingBooks.FirstOrDefault(bk => bk.Title == bkInResp.Title);
                        if (existingBook != null)
                        {
                            books.Add(existingBook);
                        }
                        else
                        {
                            // If the book doesn't exist, create a new one
                            var newBook = new Book
                            {
                                Title = bkInResp.Title ?? string.Empty,
                                ISBN = bkInResp.ISBN ?? string.Empty,
                                Author = GetAuthor(bkInResp.Authors),
                                Description = bkInResp.Description,
                                Visible = false,
                                Condition = BookCondition.Excellent
                            };
                            books.Add(newBook);
                            _context.Books.Add(newBook); // Mark it for insertion
                        }
                    }
                    await _context.SaveChangesAsync();

                    List<int> bookIdsToCheck = books.Select(bk => bk.ID).ToList();
                    List<InventoryLog> existingBooksInInventory = await _context.InventoryLogs
                        .Where(inv => bookIdsToCheck.Contains(inv.BookID))
                        .ToListAsync();
                    List<Recommendation> existingBooksInRecommendations = await _context.Recommendations
                       .Where(rec => bookIdsToCheck.Contains(rec.BookID))
                       .ToListAsync();
                    foreach (var bk in books)
                    {
                        // update inventory
                        var isBookInInventoryLogs = existingBooksInInventory.Any(inventory => inventory.BookID == bk.ID);
                        if (!isBookInInventoryLogs)
                        {
                            var newInventoryLog = new InventoryLog
                            {
                                BookID = bk.ID,
                                Quantity = bk.Quantity,
                                Type = ChangeType.InitialStock,
                                DateTime = DateTime.Now
                            };
                            _context.InventoryLogs.Add(newInventoryLog); // Mark it for insertion
                        }

                        // update recommendations
                        var isBookInRecommendations = existingBooksInRecommendations.Any(rec => rec.BookID == bk.ID);
                        if (!isBookInRecommendations)
                        {
                            var newRecommendation = new Recommendation
                            {
                                BookID = bk.ID,
                                UserID = userId
                            };
                            _context.Recommendations.Add(newRecommendation); // Mark it for insertion
                        }

                    }
                    await _context.SaveChangesAsync();

                    // Commit transaction if all operations succeed
                    await transaction.CommitAsync();

                    return Ok();
                }
                catch (Exception ex)
                {
                    // Rollback transaction if any operation fails
                    await transaction.RollbackAsync();
                    return StatusCode(500, ex.Message);
                }
                
            }
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(ex.StatusCode != null ? (int)ex.StatusCode : 500, ex.Message);
        }
        catch (JsonException ex)
        {
            _logger.LogError($"An error occurred during JSON processing: {ex.Message}", ex);
            return StatusCode(500, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError($"An unexpected error occurred: {ex.Message}", ex);
            return StatusCode(500, ex.Message);
        }
    }

    // DELETE: api/Recommendations/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRecommendation(int id)
    {
        var recommendation = await _context.Recommendations.FindAsync(id);
        if (recommendation == null)
        {
            return NotFound();
        }

        _context.Recommendations.Remove(recommendation);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool RecommendationExists(int id)
    {
        return _context.Recommendations.Any(e => e.ID == id);
    }

    private string GetAuthor(string[]? authors)
    {
        if (authors == null || authors.Length == 0)
        {
            return string.Empty;
        }
        return string.Join(", ", authors);
    }
}
