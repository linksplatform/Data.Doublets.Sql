using Platform.Data.Doublets.Sql;
using Xunit;

namespace Platform.Data.Doublets.Sql.Tests;

public class QueryPlannerTests
{
    private readonly ISqlParser _parser = new SqlParserImplementation();
    private readonly IQueryPlanner _planner = new QueryPlannerImplementation();

    [Fact]
    public void CreatePlan_SimpleSelectQuery_ShouldCreateValidPlan()
    {
        var sql = "SELECT * FROM links";
        var statement = _parser.Parse(sql);
        
        var plan = _planner.CreatePlan(statement);
        
        Assert.NotNull(plan);
        Assert.Equal(QueryType.Select, plan.Type);
        Assert.Equal("links", plan.TableName);
        Assert.NotNull(plan.Columns);
        Assert.Contains("*", plan.Columns);
    }

    [Fact]
    public void CreatePlan_SelectWithSpecificColumns_ShouldCreateValidPlan()
    {
        var sql = "SELECT id, source FROM links";
        var statement = _parser.Parse(sql);
        
        var plan = _planner.CreatePlan(statement);
        
        Assert.NotNull(plan);
        Assert.Equal(QueryType.Select, plan.Type);
        Assert.Equal("links", plan.TableName);
        Assert.NotNull(plan.Columns);
        Assert.Contains("id", plan.Columns);
        Assert.Contains("source", plan.Columns);
    }

    [Fact]
    public void CreatePlan_InsertStatement_ShouldCreateValidPlan()
    {
        var sql = "INSERT INTO links (source, target) VALUES (1, 2)";
        var statement = _parser.Parse(sql);
        
        var plan = _planner.CreatePlan(statement);
        
        Assert.NotNull(plan);
        Assert.Equal(QueryType.Insert, plan.Type);
        Assert.Equal("links", plan.TableName);
    }

    [Fact]
    public void CreatePlan_UpdateStatement_ShouldCreateValidPlan()
    {
        var sql = "UPDATE links SET target = 3 WHERE id = 1";
        var statement = _parser.Parse(sql);
        
        var plan = _planner.CreatePlan(statement);
        
        Assert.NotNull(plan);
        Assert.Equal(QueryType.Update, plan.Type);
        Assert.Equal("links", plan.TableName);
    }

    [Fact]
    public void CreatePlan_DeleteStatement_ShouldCreateValidPlan()
    {
        var sql = "DELETE FROM links WHERE id = 1";
        var statement = _parser.Parse(sql);
        
        var plan = _planner.CreatePlan(statement);
        
        Assert.NotNull(plan);
        Assert.Equal(QueryType.Delete, plan.Type);
        Assert.Equal("links", plan.TableName);
    }

    [Fact]
    public void CreatePlan_CreateTableStatement_ShouldCreateValidPlan()
    {
        var sql = "CREATE TABLE test_table (id INTEGER, name TEXT)";
        var statement = _parser.Parse(sql);
        
        var plan = _planner.CreatePlan(statement);
        
        Assert.NotNull(plan);
        Assert.Equal(QueryType.CreateTable, plan.Type);
        Assert.Equal("test_table", plan.TableName);
    }

    [Fact]
    public void CreatePlan_DropTableStatement_ShouldCreateValidPlan()
    {
        var sql = "DROP TABLE test_table";
        var statement = _parser.Parse(sql);
        
        var plan = _planner.CreatePlan(statement);
        
        Assert.NotNull(plan);
        Assert.Equal(QueryType.DropTable, plan.Type);
        Assert.Equal("test_table", plan.TableName);
    }
}