using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookonnectAPI.DTO;
using BookonnectAPI.Data;
using BookonnectAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;

namespace BookonnectAPI.Controllers
{
    [ApiController]
    [Route("/api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [Authorize(Policy = "UserClaimPolicy")] // Add an AdminClaimPolicy
    public class AffiliateDetailsController : ControllerBase
    {
        private readonly BookonnectContext _context;
        private ILogger<AffiliateDetailsController> _logger;

        public AffiliateDetailsController(BookonnectContext context, ILogger<AffiliateDetailsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/AffiliateDetails
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<AffiliateDetailsDTO>>> GetAffiliateDetails()
        {
            _logger.LogInformation("Getting affiliate details");
            try
            {

                var affiliateDetails = await _context.AffiliateDetails
                    .Include(ad => ad.Book)
                    .Select(ad => AffiliateDetails.AffiliateDetailsToDTO(ad))
                    .ToListAsync();

                return Ok(affiliateDetails);
            } catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        // GET: api/AffiliateDetails/5
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AffiliateDetailsDTO>> GetAffiliateDetails(int id)
        {
            _logger.LogInformation($"Getting affiliate details with id: {id}");
            try
            {

                var affiliateDetails = await _context.AffiliateDetails
                    .Where(ad => ad.ID == id)
                    .Include(ad => ad.Book)
                    .Select(ad => AffiliateDetails.AffiliateDetailsToDTO(ad))
                    .FirstOrDefaultAsync();

                if (affiliateDetails == null)
                {
                    return NotFound(new { Message = "Affiliate details not found."});
                }

                return Ok(affiliateDetails);
            } catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        // PUT: api/AffiliateDetails/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutAffiliateDetails(int id, AffiliateDetails affiliateDetails)
        {
            if (id != affiliateDetails.ID)
            {
                return BadRequest();
            }

            _context.Entry(affiliateDetails).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AffiliateDetailsExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }

        }

        // POST: api/AffiliateDetails
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AffiliateDetailsDTO>> PostAffiliateDetails(AffiliateDetails affiliateDetails)
        {
            _logger.LogInformation("Posting affiliate details");
            try
            {
                var affiliateDetailsExists = await _context.AffiliateDetails.AnyAsync(ad =>
                    ad.Source == affiliateDetails.Source &&
                    ad.SourceID == affiliateDetails.SourceID &&
                    ad.BookID == affiliateDetails.BookID);

                if (affiliateDetailsExists)
                {
                    _logger.LogError($"Affiliate details for book with id:{affiliateDetails.BookID}, source:{affiliateDetails.Source} and link {affiliateDetails.Link} exists");
                    return Conflict(new { Message = $"Affiliate details for book with ID:{affiliateDetails.BookID} exists" });
                }

                _context.AffiliateDetails.Add(affiliateDetails);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetAffiliateDetails", new { id = affiliateDetails.ID }, AffiliateDetails.AffiliateDetailsToDTO(affiliateDetails));
            } catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPatch("{id}")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AffiliateDetailsDTO>> PatchAffiliateDetails(int id, [FromBody] JsonPatchDocument<AffiliateDetails> patchDoc)
        {
            if (patchDoc == null)
            {
                return BadRequest(ModelState);
            }

            var affiliateDetails = await _context.AffiliateDetails
                .Where(ad => ad.ID == id)
                .Include(ad => ad.Book)
                .FirstOrDefaultAsync();

            if (affiliateDetails == null)
            {
                return NotFound(new { Message = "Affiliate details not found." });
            }

            patchDoc.ApplyTo(affiliateDetails, ModelState);
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Update(affiliateDetails);
            try
            {
                await _context.SaveChangesAsync();
                return Ok(AffiliateDetails.AffiliateDetailsToDTO(affiliateDetails));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AffiliateDetailsExists(id))
                {
                    return NotFound(new { Message = "Affiliate details not found." });
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        // DELETE: api/AffiliateDetails/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteAffiliateDetails(int id)
        {
            try
            {
                var affiliateDetails = await _context.AffiliateDetails.FindAsync(id);
                if (affiliateDetails == null)
                {
                    return NotFound(new { Message = "Affiliate details not found." });
                }

                _context.AffiliateDetails.Remove(affiliateDetails);
                await _context.SaveChangesAsync();

                return NoContent();
            } catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        private bool AffiliateDetailsExists(int id)
        {
            return _context.AffiliateDetails.Any(e => e.ID == id);
        }
    }
}
