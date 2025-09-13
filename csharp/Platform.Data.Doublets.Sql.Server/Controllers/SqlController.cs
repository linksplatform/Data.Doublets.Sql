using Microsoft.AspNetCore.Mvc;
using Platform.Data.Doublets.Sql;

namespace Platform.Data.Doublets.Sql.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SqlController : ControllerBase
{
    private readonly ISqlService _sqlService;
    private readonly ILogger<SqlController> _logger;

    public SqlController(ISqlService sqlService, ILogger<SqlController> logger)
    {
        _sqlService = sqlService ?? throw new ArgumentNullException(nameof(sqlService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteSql([FromBody] SqlRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Sql))
        {
            return BadRequest(new { error = "SQL statement is required" });
        }

        _logger.LogInformation("Executing SQL: {Sql}", request.Sql);

        try
        {
            var result = await _sqlService.ExecuteSqlAsync(request.Sql);
            
            if (result.Success)
            {
                return Ok(new SqlResponse
                {
                    Success = true,
                    Rows = result.Rows,
                    AffectedRows = result.AffectedRows,
                    ExecutionTimeMs = 0 
                });
            }
            else
            {
                return BadRequest(new SqlResponse
                {
                    Success = false,
                    ErrorMessage = result.ErrorMessage,
                    AffectedRows = 0
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing SQL: {Sql}", request.Sql);
            return StatusCode(500, new SqlResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                AffectedRows = 0
            });
        }
    }

    [HttpGet("tables")]
    public async Task<IActionResult> GetTables([FromServices] IVirtualTableManager virtualTableManager)
    {
        try
        {
            var tables = new List<string> { "links" };
            tables.AddRange(virtualTableManager.GetTableNames());

            return Ok(new { tables });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tables");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}

public record SqlRequest(string Sql);

public record SqlResponse
{
    public bool Success { get; set; }
    public IReadOnlyList<IReadOnlyDictionary<string, object>>? Rows { get; set; }
    public int AffectedRows { get; set; }
    public string? ErrorMessage { get; set; }
    public long ExecutionTimeMs { get; set; }
}