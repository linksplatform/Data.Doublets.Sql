using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Platform.Data.Doublets;
using System.Numerics;

namespace Platform.Data.Doublets.Sql;

public interface IDoubletsQueryExecutor
{
    Task<QueryResult> ExecuteAsync(QueryPlan plan);
}

public record QueryResult(
    bool Success,
    IReadOnlyList<IReadOnlyDictionary<string, object>>? Rows,
    int AffectedRows,
    string? ErrorMessage
);

public class DoubletsQueryExecutor<TLinkAddress> : IDoubletsQueryExecutor
    where TLinkAddress : struct, IUnsignedNumber<TLinkAddress>
{
    private readonly ILinks<TLinkAddress> _links;
    private readonly IVirtualTableManager _virtualTableManager;

    public DoubletsQueryExecutor(ILinks<TLinkAddress> links, IVirtualTableManager virtualTableManager)
    {
        _links = links ?? throw new ArgumentNullException(nameof(links));
        _virtualTableManager = virtualTableManager ?? throw new ArgumentNullException(nameof(virtualTableManager));
    }

    public async Task<QueryResult> ExecuteAsync(QueryPlan plan)
    {
        try
        {
            return plan.Type switch
            {
                QueryType.Select => await ExecuteSelectAsync(plan),
                QueryType.Insert => await ExecuteInsertAsync(plan),
                QueryType.Update => await ExecuteUpdateAsync(plan),
                QueryType.Delete => await ExecuteDeleteAsync(plan),
                QueryType.CreateTable => await ExecuteCreateTableAsync(plan),
                QueryType.DropTable => await ExecuteDropTableAsync(plan),
                _ => new QueryResult(false, null, 0, $"Unsupported query type: {plan.Type}")
            };
        }
        catch (Exception ex)
        {
            return new QueryResult(false, null, 0, ex.Message);
        }
    }

    private async Task<QueryResult> ExecuteSelectAsync(QueryPlan plan)
    {
        if (plan.TableName == null)
            return new QueryResult(false, null, 0, "Table name is required for SELECT queries");

        if (plan.TableName.Equals("links", StringComparison.OrdinalIgnoreCase))
        {
            return await ExecuteLinksSelectAsync(plan);
        }
        else if (_virtualTableManager.TableExists(plan.TableName))
        {
            return await _virtualTableManager.ExecuteSelectAsync(plan.TableName, plan);
        }
        else
        {
            return new QueryResult(false, null, 0, $"Table '{plan.TableName}' does not exist");
        }
    }

    private async Task<QueryResult> ExecuteLinksSelectAsync(QueryPlan plan)
    {
        var rows = new List<IReadOnlyDictionary<string, object>>();
        var any = _links.Constants.Any;

        var query = new Link<TLinkAddress>(any, any, any);
        
        if (plan.Conditions != null)
        {
            foreach (var condition in plan.Conditions)
            {
                if (condition.Column.Equals("id", StringComparison.OrdinalIgnoreCase) && 
                    condition.Operator == ConditionOperator.Equal &&
                    TLinkAddress.TryParse(condition.Value.ToString(), out var linkId))
                {
                    query = new Link<TLinkAddress>(linkId, any, any);
                    break;
                }
                else if (condition.Column.Equals("source", StringComparison.OrdinalIgnoreCase) && 
                         condition.Operator == ConditionOperator.Equal &&
                         TLinkAddress.TryParse(condition.Value.ToString(), out var source))
                {
                    query = new Link<TLinkAddress>(query.Index, source, query.Target);
                }
                else if (condition.Column.Equals("target", StringComparison.OrdinalIgnoreCase) && 
                         condition.Operator == ConditionOperator.Equal &&
                         TLinkAddress.TryParse(condition.Value.ToString(), out var target))
                {
                    query = new Link<TLinkAddress>(query.Index, query.Source, target);
                }
            }
        }

        var count = 0;
        var maxCount = plan.Limit ?? 1000; 
        var skipCount = plan.Offset ?? 0;

        _links.Each(link =>
        {
            if (count >= skipCount)
            {
                var row = new Dictionary<string, object>
                {
                    ["id"] = link[_links.Constants.IndexPart].ToString()!,
                    ["source"] = link[_links.Constants.SourcePart].ToString()!,
                    ["target"] = link[_links.Constants.TargetPart].ToString()!
                };

                if (plan.Columns == null || plan.Columns.Contains("*") || plan.Columns.All(c => row.ContainsKey(c.ToLowerInvariant())))
                {
                    if (plan.Columns != null && !plan.Columns.Contains("*"))
                    {
                        var filteredRow = new Dictionary<string, object>();
                        foreach (var column in plan.Columns)
                        {
                            if (row.TryGetValue(column.ToLowerInvariant(), out var value))
                                filteredRow[column] = value;
                        }
                        rows.Add(filteredRow);
                    }
                    else
                    {
                        rows.Add(row);
                    }
                }
            }

            count++;
            return rows.Count < maxCount ? _links.Constants.Continue : _links.Constants.Break;
        }, query.Index, query.Source, query.Target);

        return await Task.FromResult(new QueryResult(true, rows, rows.Count, null));
    }

    private async Task<QueryResult> ExecuteInsertAsync(QueryPlan plan)
    {
        if (plan.TableName == null)
            return new QueryResult(false, null, 0, "Table name is required for INSERT queries");

        if (plan.TableName.Equals("links", StringComparison.OrdinalIgnoreCase))
        {
            return await ExecuteLinksInsertAsync(plan);
        }
        else if (_virtualTableManager.TableExists(plan.TableName))
        {
            return await _virtualTableManager.ExecuteInsertAsync(plan.TableName, plan);
        }
        else
        {
            return new QueryResult(false, null, 0, $"Table '{plan.TableName}' does not exist");
        }
    }

    private async Task<QueryResult> ExecuteLinksInsertAsync(QueryPlan plan)
    {
        try
        {
            if (plan.Parameters == null)
                return new QueryResult(false, null, 0, "Parameters are required for INSERT queries");

            var source = _links.Constants.Null;
            var target = _links.Constants.Null;

            if (plan.Parameters.TryGetValue("source", out var sourceValue) && 
                TLinkAddress.TryParse(sourceValue.ToString(), out var parsedSource))
            {
                source = parsedSource;
            }

            if (plan.Parameters.TryGetValue("target", out var targetValue) && 
                TLinkAddress.TryParse(targetValue.ToString(), out var parsedTarget))
            {
                target = parsedTarget;
            }

            var newLink = _links.Create(source, target);
            return await Task.FromResult(new QueryResult(true, null, 1, null));
        }
        catch (Exception ex)
        {
            return new QueryResult(false, null, 0, ex.Message);
        }
    }

    private async Task<QueryResult> ExecuteUpdateAsync(QueryPlan plan)
    {
        if (plan.TableName == null)
            return new QueryResult(false, null, 0, "Table name is required for UPDATE queries");

        if (plan.TableName.Equals("links", StringComparison.OrdinalIgnoreCase))
        {
            return await ExecuteLinksUpdateAsync(plan);
        }
        else if (_virtualTableManager.TableExists(plan.TableName))
        {
            return await _virtualTableManager.ExecuteUpdateAsync(plan.TableName, plan);
        }
        else
        {
            return new QueryResult(false, null, 0, $"Table '{plan.TableName}' does not exist");
        }
    }

    private async Task<QueryResult> ExecuteLinksUpdateAsync(QueryPlan plan)
    {
        return await Task.FromResult(new QueryResult(false, null, 0, "UPDATE operations on links table are not yet implemented"));
    }

    private async Task<QueryResult> ExecuteDeleteAsync(QueryPlan plan)
    {
        if (plan.TableName == null)
            return new QueryResult(false, null, 0, "Table name is required for DELETE queries");

        if (plan.TableName.Equals("links", StringComparison.OrdinalIgnoreCase))
        {
            return await ExecuteLinksDeleteAsync(plan);
        }
        else if (_virtualTableManager.TableExists(plan.TableName))
        {
            return await _virtualTableManager.ExecuteDeleteAsync(plan.TableName, plan);
        }
        else
        {
            return new QueryResult(false, null, 0, $"Table '{plan.TableName}' does not exist");
        }
    }

    private async Task<QueryResult> ExecuteLinksDeleteAsync(QueryPlan plan)
    {
        try
        {
            var deletedCount = 0;

            if (plan.Conditions != null)
            {
                foreach (var condition in plan.Conditions)
                {
                    if (condition.Column.Equals("id", StringComparison.OrdinalIgnoreCase) && 
                        condition.Operator == ConditionOperator.Equal &&
                        TLinkAddress.TryParse(condition.Value.ToString(), out var linkId))
                    {
                        if (_links.Exists(linkId))
                        {
                            _links.Delete(linkId);
                            deletedCount++;
                        }
                        break;
                    }
                }
            }

            return await Task.FromResult(new QueryResult(true, null, deletedCount, null));
        }
        catch (Exception ex)
        {
            return new QueryResult(false, null, 0, ex.Message);
        }
    }

    private async Task<QueryResult> ExecuteCreateTableAsync(QueryPlan plan)
    {
        if (plan.TableName == null)
            return new QueryResult(false, null, 0, "Table name is required for CREATE TABLE queries");

        if (plan.TableName.Equals("links", StringComparison.OrdinalIgnoreCase))
        {
            return new QueryResult(false, null, 0, "Links table already exists and cannot be created");
        }

        var success = await _virtualTableManager.CreateTableAsync(plan.TableName, plan);
        return new QueryResult(success, null, success ? 1 : 0, success ? null : "Failed to create table");
    }

    private async Task<QueryResult> ExecuteDropTableAsync(QueryPlan plan)
    {
        if (plan.TableName == null)
            return new QueryResult(false, null, 0, "Table name is required for DROP TABLE queries");

        if (plan.TableName.Equals("links", StringComparison.OrdinalIgnoreCase))
        {
            return new QueryResult(false, null, 0, "Links table cannot be dropped");
        }

        if (!_virtualTableManager.TableExists(plan.TableName))
        {
            return new QueryResult(false, null, 0, $"Table '{plan.TableName}' does not exist");
        }

        var success = await _virtualTableManager.DropTableAsync(plan.TableName);
        return new QueryResult(success, null, success ? 1 : 0, success ? null : "Failed to drop table");
    }
}