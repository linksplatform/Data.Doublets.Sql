using System;
using SqlParser;
using SqlParser.Ast;

namespace Platform.Data.Doublets.Sql;

public interface ISqlParser
{
    Statement Parse(string sql);
    bool TryParse(string sql, out Statement? statement, out Exception? error);
}

public class SqlParserImplementation : ISqlParser
{
    private readonly Parser _parser;

    public SqlParserImplementation()
    {
        _parser = new Parser();
    }

    public Statement Parse(string sql)
    {
        var statements = _parser.ParseSql(sql);
        if (statements.Count == 0)
            throw new ArgumentException("No SQL statements found", nameof(sql));
        
        return statements[0];
    }

    public bool TryParse(string sql, out Statement? statement, out Exception? error)
    {
        try
        {
            statement = Parse(sql);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            statement = null;
            error = ex;
            return false;
        }
    }
}