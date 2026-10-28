
namespace BookonnectAPI.Controllers.V2
{
  using BookonnectAPI.Models;
  using BookonnectCore.Models;
  using BookonnectCore.Services.Interfaces;
  using Microsoft.AspNetCore.Authorization;
  using Microsoft.AspNetCore.Mvc;

  [ApiController]
  [Authorize(Policy = "UserClaimPolicy")]
  [Route("api/v2/[controller]")]
  // [Authorize(Roles = "Admin")]
  public class SyncController : ControllerBase
  {
    private readonly ILogger<SyncController> _logger;
    private readonly IDatabaseSyncService _databaseSyncService;
    private readonly IRelationalOperationsService _relationalOperationsService;
    private readonly IVectorOperationsService _vectorOperationsService;

    public SyncController(ILogger<SyncController> logger, IDatabaseSyncService databaseSyncService, IRelationalOperationsService relationalOperationsService, IVectorOperationsService vectorOperationsService)
    {
      _logger = logger;
      _databaseSyncService = databaseSyncService;
      _relationalOperationsService = relationalOperationsService;
      _vectorOperationsService = vectorOperationsService;
    }

    [HttpPost("sync/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(StatusCodes.Status499ClientClosedRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SyncFromCsv([FromBody] CsvSyncRequest request)
    {
      if (!ModelState.IsValid)
      {
        return BadRequest(ModelState);
      }
      try
      {
        ArgumentNullException.ThrowIfNull(request.FilePath, nameof(request.FilePath));
        await _relationalOperationsService.EnsureDatabaseInitializedAsync();
        await _vectorOperationsService.InitializeAsync();
        await _databaseSyncService.SyncFromCsvAsync(request.FilePath);
        return Ok(new { Message = "CSV sync completed successfully" });
      }
      catch (OperationCanceledException ex)
      {
        _logger.LogError(ex, "CSV sync operation was canceled");
        return StatusCode(499, new { Message = "CSV sync failed due to client timeout.", Error = ex.Message });
      }
    }

    [HttpPost("sync/full")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(StatusCodes.Status499ClientClosedRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SyncDatabase()
    {
      if (!ModelState.IsValid)
      {
        return BadRequest(ModelState);
      }
      try
      {
        await _relationalOperationsService.EnsureDatabaseInitializedAsync();
        await _vectorOperationsService.InitializeAsync();
        await _databaseSyncService.SyncRelationalToVectorDBAsync();
        return Ok(new { Message = "Full database sync completed successfully" });
      }
      catch (OperationCanceledException ex)
      {
        _logger.LogError(ex, "Full database sync operation was canceled");
        return StatusCode(499, new { Message = "Full database sync failed due to client timeout.", Error = ex.Message });
      }
    }

    [HttpGet("sync/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(StatusCodes.Status499ClientClosedRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SyncStatus>> GetSyncStatus()
    {
      try
      {
        await _relationalOperationsService.EnsureDatabaseInitializedAsync();
        await _vectorOperationsService.InitializeAsync();
        var status = await _databaseSyncService.GetSyncStatusAsync();
        return Ok(status);
      }
      catch (OperationCanceledException ex)
      {
        _logger.LogError(ex, "Sync status retrieval operation was canceled");
        return StatusCode(499, new { Message = "Failed to retrieve sync status due to client timeout.", Error = ex.Message });
      }
    }
  }
}