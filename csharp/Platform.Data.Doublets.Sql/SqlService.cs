using System;
using System.Threading.Tasks;
using Platform.Data.Doublets;
using System.Numerics;

namespace Platform.Data.Doublets.Sql;

public interface ISqlService
{
    Task<QueryResult> ExecuteSqlAsync(string sql);
}

public class SqlService<TLinkAddress> : ISqlService
    where TLinkAddress : struct, IUnsignedNumber<TLinkAddress>
{
    private readonly ISqlParser _parser;
    private readonly IQueryPlanner _planner;
    private readonly IDoubletsQueryExecutor _executor;

    public SqlService(ISqlParser parser, IQueryPlanner planner, IDoubletsQueryExecutor executor)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _planner = planner ?? throw new ArgumentNullException(nameof(planner));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    public async Task<QueryResult> ExecuteSqlAsync(string sql)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sql))
                return new QueryResult(false, null, 0, "SQL statement cannot be empty");

            var statement = _parser.Parse(sql);
            var plan = _planner.CreatePlan(statement);
            var result = await _executor.ExecuteAsync(plan);

            return result;
        }
        catch (Exception ex)
        {
            return new QueryResult(false, null, 0, $"SQL execution error: {ex.Message}");
        }
    }
}