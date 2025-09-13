using Platform.Data.Doublets.Sql;
using SqlParser.Ast;
using Xunit;

namespace Platform.Data.Doublets.Sql.Tests;

public class SqlParserTests
{
    private readonly ISqlParser _parser = new SqlParserImplementation();

    [Fact]
    public void Parse_SimpleSelectQuery_ShouldSucceed()
    {
        var sql = "SELECT * FROM links";
        
        var statement = _parser.Parse(sql);
        
        Assert.NotNull(statement);
        Assert.IsType<Statement.Query>(statement);
    }

    [Fact]
    public void Parse_SelectWithConditions_ShouldSucceed()
    {
        var sql = "SELECT id, source, target FROM links WHERE id = 1";
        
        var statement = _parser.Parse(sql);
        
        Assert.NotNull(statement);
        Assert.IsType<Statement.Query>(statement);
    }

    [Fact]
    public void Parse_InsertStatement_ShouldSucceed()
    {
        var sql = "INSERT INTO links (source, target) VALUES (1, 2)";
        
        var statement = _parser.Parse(sql);
        
        Assert.NotNull(statement);
        Assert.IsType<Statement.Insert>(statement);
    }

    [Fact]
    public void Parse_UpdateStatement_ShouldSucceed()
    {
        var sql = "UPDATE links SET target = 3 WHERE id = 1";
        
        var statement = _parser.Parse(sql);
        
        Assert.NotNull(statement);
        Assert.IsType<Statement.Update>(statement);
    }

    [Fact]
    public void Parse_DeleteStatement_ShouldSucceed()
    {
        var sql = "DELETE FROM links WHERE id = 1";
        
        var statement = _parser.Parse(sql);
        
        Assert.NotNull(statement);
        Assert.IsType<Statement.Delete>(statement);
    }

    [Fact]
    public void Parse_CreateTableStatement_ShouldSucceed()
    {
        var sql = "CREATE TABLE test_table (id INTEGER, name TEXT)";
        
        var statement = _parser.Parse(sql);
        
        Assert.NotNull(statement);
        Assert.IsType<Statement.CreateTable>(statement);
    }

    [Fact]
    public void Parse_DropTableStatement_ShouldSucceed()
    {
        var sql = "DROP TABLE test_table";
        
        var statement = _parser.Parse(sql);
        
        Assert.NotNull(statement);
        Assert.IsType<Statement.Drop>(statement);
    }

    [Fact]
    public void TryParse_InvalidSql_ShouldReturnFalse()
    {
        var sql = "INVALID SQL STATEMENT";
        
        var result = _parser.TryParse(sql, out var statement, out var error);
        
        Assert.False(result);
        Assert.Null(statement);
        Assert.NotNull(error);
    }

    [Fact]
    public void Parse_EmptyString_ShouldThrow()
    {
        var sql = "";
        
        Assert.Throws<ArgumentException>(() => _parser.Parse(sql));
    }
}