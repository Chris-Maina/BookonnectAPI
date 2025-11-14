using BookonnectAPI.Models;
using BookonnectCore.Models;
using BookonnectCore.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookonnectAPI.Controllers.V2
{
  [ApiController]
  [Route("api/v2/[controller]")]
  // [Authorize(Roles = "Admin")]
  public class SyncController : ControllerBase
  {
    private readonly ILogger<SyncController> _logger;
    private readonly IDatabaseSyncService _databaseSyncService;

    public SyncController(ILogger<SyncController> logger, IDatabaseSyncService databaseSyncService)
    {
      _logger = logger;
      _databaseSyncService = databaseSyncService;
    }

    [HttpPost("sync/csv")]
    public async Task<IActionResult> SyncFromCsv([FromBody] CsvSyncRequest request)
    {
      if (!ModelState.IsValid)
      {
        return BadRequest(ModelState);
      }
      try
      {
        ArgumentNullException.ThrowIfNull(request.FilePath, nameof(request.FilePath));
        await _databaseSyncService.SyncFromCsvAsync(request.FilePath);
        return Ok(new { Message = "CSV sync completed successfully" });
      }
      catch (ArgumentNullException ex)
      {
        _logger.LogError(ex, "Sync request data is null");
        return BadRequest(new { Message = "Sync request data cannot be null." });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "An error occurred while syncing CSV data");
        return StatusCode(500, new { Message = "CSV sync failed.", Error = ex.Message });
      }
    }

    [HttpPost("sync/full")]
    public async Task<IActionResult> SyncDatabase()
    {
      if (!ModelState.IsValid)
      {
        return BadRequest(ModelState);
      }
      try
      {
        await _databaseSyncService.SyncRelationalToVectorDBAsync();
        return Ok(new { Message = "Full database sync completed successfully" });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "An error occurred during full database sync");
        return StatusCode(500, new { Message = "Full database sync failed.", Error = ex.Message });
      }
    }

    [HttpGet("sync/status")]
    public async Task<ActionResult<SyncStatus>> GetSyncStatus()
    {
      try
      {
        var status = await _databaseSyncService.GetSyncStatusAsync();
        return Ok(status);
      }
      catch (Exception ex)
      {
        return StatusCode(500, new { Message = "Failed to retrieve sync status.", Error = ex.Message });
      }
    }
  }
}