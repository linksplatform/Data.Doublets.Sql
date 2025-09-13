using Platform.Data.Doublets;
using Platform.Data.Doublets.Memory.United.Generic;
using Platform.Data.Doublets.Sql;
using Platform.Memory;
using Xunit;

namespace Platform.Data.Doublets.Sql.Tests;

public class SqlServiceIntegrationTests : IDisposable
{
    private readonly ISqlService _sqlService;
    private readonly ILinks<ulong> _links;
    private readonly TemporaryFile _tempFile;

    public SqlServiceIntegrationTests()
    {
        _tempFile = new TemporaryFile();
        var memory = new FileMappedResizableDirectMemory(_tempFile.Filename);
        _links = new UnitedMemoryLinks<ulong>(memory);
        
        var virtualTableManager = new VirtualTableManager();
        var parser = new SqlParserImplementation();
        var planner = new QueryPlannerImplementation();
        var executor = new DoubletsQueryExecutor<ulong>(_links, virtualTableManager);
        
        _sqlService = new SqlService<ulong>(parser, planner, executor);
    }

    [Fact]
    public async Task ExecuteSqlAsync_SelectFromLinks_ShouldReturnResults()
    {
        _links.Create(1, 2);
        _links.Create(2, 3);
        
        var sql = "SELECT * FROM links";
        var result = await _sqlService.ExecuteSqlAsync(sql);
        
        Assert.True(result.Success);
        Assert.NotNull(result.Rows);
        Assert.True(result.Rows.Count >= 2);
    }

    [Fact]
    public async Task ExecuteSqlAsync_SelectWithCondition_ShouldFilterResults()
    {
        var linkId = _links.Create(1, 2);
        
        var sql = $"SELECT * FROM links WHERE id = {linkId}";
        var result = await _sqlService.ExecuteSqlAsync(sql);
        
        Assert.True(result.Success);
        Assert.NotNull(result.Rows);
        Assert.Single(result.Rows);
        Assert.Equal(linkId.ToString(), result.Rows[0]["id"]);
    }

    [Fact]
    public async Task ExecuteSqlAsync_CreateTable_ShouldSucceed()
    {
        var sql = "CREATE TABLE test_table (id INTEGER, name TEXT)";
        var result = await _sqlService.ExecuteSqlAsync(sql);
        
        Assert.True(result.Success);
        Assert.Equal(1, result.AffectedRows);
    }

    [Fact]
    public async Task ExecuteSqlAsync_InsertIntoVirtualTable_ShouldSucceed()
    {
        await _sqlService.ExecuteSqlAsync("CREATE TABLE test_table (id INTEGER, name TEXT)");
        
        var sql = "INSERT INTO test_table (name) VALUES ('test')";
        var result = await _sqlService.ExecuteSqlAsync(sql);
        
        Assert.True(result.Success);
        Assert.Equal(1, result.AffectedRows);
    }

    [Fact]
    public async Task ExecuteSqlAsync_SelectFromVirtualTable_ShouldReturnData()
    {
        await _sqlService.ExecuteSqlAsync("CREATE TABLE test_table (id INTEGER, name TEXT)");
        await _sqlService.ExecuteSqlAsync("INSERT INTO test_table (name) VALUES ('test')");
        
        var sql = "SELECT * FROM test_table";
        var result = await _sqlService.ExecuteSqlAsync(sql);
        
        Assert.True(result.Success);
        Assert.NotNull(result.Rows);
        Assert.Single(result.Rows);
        Assert.Equal(1, result.Rows[0]["id"]);
        Assert.Equal("test", result.Rows[0]["name"]);
    }

    [Fact]
    public async Task ExecuteSqlAsync_DropTable_ShouldSucceed()
    {
        await _sqlService.ExecuteSqlAsync("CREATE TABLE test_table (id INTEGER, name TEXT)");
        
        var sql = "DROP TABLE test_table";
        var result = await _sqlService.ExecuteSqlAsync(sql);
        
        Assert.True(result.Success);
        Assert.Equal(1, result.AffectedRows);
    }

    [Fact]
    public async Task ExecuteSqlAsync_DeleteFromLinks_ShouldRemoveLink()
    {
        var linkId = _links.Create(1, 2);
        
        var sql = $"DELETE FROM links WHERE id = {linkId}";
        var result = await _sqlService.ExecuteSqlAsync(sql);
        
        Assert.True(result.Success);
        Assert.Equal(1, result.AffectedRows);
        Assert.False(_links.Exists(linkId));
    }

    [Fact]
    public async Task ExecuteSqlAsync_InvalidSql_ShouldReturnError()
    {
        var sql = "INVALID SQL STATEMENT";
        var result = await _sqlService.ExecuteSqlAsync(sql);
        
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteSqlAsync_EmptyQuery_ShouldReturnError()
    {
        var sql = "";
        var result = await _sqlService.ExecuteSqlAsync(sql);
        
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    public void Dispose()
    {
        _links?.Dispose();
        _tempFile?.Dispose();
    }
}

public class TemporaryFile : IDisposable
{
    public string Filename { get; }

    public TemporaryFile()
    {
        Filename = Path.GetTempFileName();
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(Filename))
            {
                File.Delete(Filename);
            }
        }
        catch
        {
        }
    }
}