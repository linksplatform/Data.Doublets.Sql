using Platform.Data.Doublets.Sql;
using Xunit;

namespace Platform.Data.Doublets.Sql.Tests;

public class VirtualTableManagerTests
{
    private readonly IVirtualTableManager _tableManager = new VirtualTableManager();

    [Fact]
    public async Task CreateTableAsync_NewTable_ShouldSucceed()
    {
        var tableName = "test_table";
        var plan = new QueryPlan(QueryType.CreateTable, tableName, null, null, null, null, null, null);
        
        var result = await _tableManager.CreateTableAsync(tableName, plan);
        
        Assert.True(result);
        Assert.True(_tableManager.TableExists(tableName));
    }

    [Fact]
    public async Task CreateTableAsync_ExistingTable_ShouldFail()
    {
        var tableName = "test_table";
        var plan = new QueryPlan(QueryType.CreateTable, tableName, null, null, null, null, null, null);
        
        await _tableManager.CreateTableAsync(tableName, plan);
        var result = await _tableManager.CreateTableAsync(tableName, plan);
        
        Assert.False(result);
    }

    [Fact]
    public async Task DropTableAsync_ExistingTable_ShouldSucceed()
    {
        var tableName = "test_table";
        var plan = new QueryPlan(QueryType.CreateTable, tableName, null, null, null, null, null, null);
        
        await _tableManager.CreateTableAsync(tableName, plan);
        var result = await _tableManager.DropTableAsync(tableName);
        
        Assert.True(result);
        Assert.False(_tableManager.TableExists(tableName));
    }

    [Fact]
    public async Task DropTableAsync_NonExistentTable_ShouldFail()
    {
        var tableName = "non_existent_table";
        
        var result = await _tableManager.DropTableAsync(tableName);
        
        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteInsertAsync_ValidTable_ShouldInsertRow()
    {
        var tableName = "test_table";
        var createPlan = new QueryPlan(QueryType.CreateTable, tableName, null, null, null, null, null, null);
        await _tableManager.CreateTableAsync(tableName, createPlan);
        
        var insertPlan = new QueryPlan(
            QueryType.Insert,
            tableName,
            null,
            new Dictionary<string, object> { ["name"] = "test", ["value"] = 42 },
            null,
            null,
            null,
            null
        );
        
        var result = await _tableManager.ExecuteInsertAsync(tableName, insertPlan);
        
        Assert.True(result.Success);
        Assert.Equal(1, result.AffectedRows);
    }

    [Fact]
    public async Task ExecuteSelectAsync_WithData_ShouldReturnRows()
    {
        var tableName = "test_table";
        var createPlan = new QueryPlan(QueryType.CreateTable, tableName, null, null, null, null, null, null);
        await _tableManager.CreateTableAsync(tableName, createPlan);
        
        var insertPlan = new QueryPlan(
            QueryType.Insert,
            tableName,
            null,
            new Dictionary<string, object> { ["name"] = "test", ["value"] = 42 },
            null,
            null,
            null,
            null
        );
        await _tableManager.ExecuteInsertAsync(tableName, insertPlan);
        
        var selectPlan = new QueryPlan(QueryType.Select, tableName, null, null, null, null, null, null);
        var result = await _tableManager.ExecuteSelectAsync(tableName, selectPlan);
        
        Assert.True(result.Success);
        Assert.NotNull(result.Rows);
        Assert.Single(result.Rows);
        Assert.Equal(1, result.Rows[0]["id"]);
        Assert.Equal("test", result.Rows[0]["name"]);
        Assert.Equal(42, result.Rows[0]["value"]);
    }

    [Fact]
    public async Task ExecuteSelectAsync_WithConditions_ShouldFilterRows()
    {
        var tableName = "test_table";
        var createPlan = new QueryPlan(QueryType.CreateTable, tableName, null, null, null, null, null, null);
        await _tableManager.CreateTableAsync(tableName, createPlan);
        
        var insertPlan1 = new QueryPlan(
            QueryType.Insert,
            tableName,
            null,
            new Dictionary<string, object> { ["name"] = "test1", ["value"] = 10 },
            null,
            null,
            null,
            null
        );
        var insertPlan2 = new QueryPlan(
            QueryType.Insert,
            tableName,
            null,
            new Dictionary<string, object> { ["name"] = "test2", ["value"] = 20 },
            null,
            null,
            null,
            null
        );
        
        await _tableManager.ExecuteInsertAsync(tableName, insertPlan1);
        await _tableManager.ExecuteInsertAsync(tableName, insertPlan2);
        
        var conditions = new List<QueryCondition>
        {
            new("value", ConditionOperator.Equal, 20)
        };
        
        var selectPlan = new QueryPlan(QueryType.Select, tableName, null, null, conditions, null, null, null);
        var result = await _tableManager.ExecuteSelectAsync(tableName, selectPlan);
        
        Assert.True(result.Success);
        Assert.NotNull(result.Rows);
        Assert.Single(result.Rows);
        Assert.Equal("test2", result.Rows[0]["name"]);
    }

    [Fact]
    public async Task ExecuteDeleteAsync_WithConditions_ShouldDeleteMatchingRows()
    {
        var tableName = "test_table";
        var createPlan = new QueryPlan(QueryType.CreateTable, tableName, null, null, null, null, null, null);
        await _tableManager.CreateTableAsync(tableName, createPlan);
        
        var insertPlan = new QueryPlan(
            QueryType.Insert,
            tableName,
            null,
            new Dictionary<string, object> { ["name"] = "test", ["value"] = 42 },
            null,
            null,
            null,
            null
        );
        await _tableManager.ExecuteInsertAsync(tableName, insertPlan);
        
        var conditions = new List<QueryCondition>
        {
            new("name", ConditionOperator.Equal, "test")
        };
        
        var deletePlan = new QueryPlan(QueryType.Delete, tableName, null, null, conditions, null, null, null);
        var result = await _tableManager.ExecuteDeleteAsync(tableName, deletePlan);
        
        Assert.True(result.Success);
        Assert.Equal(1, result.AffectedRows);
        
        var selectPlan = new QueryPlan(QueryType.Select, tableName, null, null, null, null, null, null);
        var selectResult = await _tableManager.ExecuteSelectAsync(tableName, selectPlan);
        Assert.Empty(selectResult.Rows!);
    }

    [Fact]
    public void TableExists_NonExistentTable_ShouldReturnFalse()
    {
        var result = _tableManager.TableExists("non_existent_table");
        
        Assert.False(result);
    }

    [Fact]
    public void GetTableNames_EmptyManager_ShouldReturnEmptyList()
    {
        var result = _tableManager.GetTableNames();
        
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTableNames_WithTables_ShouldReturnTableNames()
    {
        var tableName1 = "table1";
        var tableName2 = "table2";
        
        var plan1 = new QueryPlan(QueryType.CreateTable, tableName1, null, null, null, null, null, null);
        var plan2 = new QueryPlan(QueryType.CreateTable, tableName2, null, null, null, null, null, null);
        
        await _tableManager.CreateTableAsync(tableName1, plan1);
        await _tableManager.CreateTableAsync(tableName2, plan2);
        
        var result = _tableManager.GetTableNames();
        
        Assert.Contains(tableName1.ToLowerInvariant(), result);
        Assert.Contains(tableName2.ToLowerInvariant(), result);
    }
}