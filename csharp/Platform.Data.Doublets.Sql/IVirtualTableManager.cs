using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.Data.Doublets.Sql;

public interface IVirtualTableManager
{
    bool TableExists(string tableName);
    Task<bool> CreateTableAsync(string tableName, QueryPlan plan);
    Task<bool> DropTableAsync(string tableName);
    Task<QueryResult> ExecuteSelectAsync(string tableName, QueryPlan plan);
    Task<QueryResult> ExecuteInsertAsync(string tableName, QueryPlan plan);
    Task<QueryResult> ExecuteUpdateAsync(string tableName, QueryPlan plan);
    Task<QueryResult> ExecuteDeleteAsync(string tableName, QueryPlan plan);
    IReadOnlyList<string> GetTableNames();
}

public record VirtualTableSchema(
    string Name,
    IReadOnlyDictionary<string, VirtualColumnType> Columns,
    DateTime CreatedAt
);

public enum VirtualColumnType
{
    Integer,
    Text,
    Real,
    Blob,
    Boolean
}

public class VirtualTableManager : IVirtualTableManager
{
    private readonly Dictionary<string, VirtualTableSchema> _tables = new();
    private readonly Dictionary<string, List<Dictionary<string, object>>> _tableData = new();

    public bool TableExists(string tableName)
    {
        return _tables.ContainsKey(tableName.ToLowerInvariant());
    }

    public Task<bool> CreateTableAsync(string tableName, QueryPlan plan)
    {
        var normalizedName = tableName.ToLowerInvariant();
        
        if (_tables.ContainsKey(normalizedName))
            return Task.FromResult(false);

        var columns = new Dictionary<string, VirtualColumnType>
        {
            ["id"] = VirtualColumnType.Integer
        };

        var schema = new VirtualTableSchema(tableName, columns, DateTime.UtcNow);
        _tables[normalizedName] = schema;
        _tableData[normalizedName] = new List<Dictionary<string, object>>();

        return Task.FromResult(true);
    }

    public Task<bool> DropTableAsync(string tableName)
    {
        var normalizedName = tableName.ToLowerInvariant();
        
        if (!_tables.ContainsKey(normalizedName))
            return Task.FromResult(false);

        _tables.Remove(normalizedName);
        _tableData.Remove(normalizedName);

        return Task.FromResult(true);
    }

    public Task<QueryResult> ExecuteSelectAsync(string tableName, QueryPlan plan)
    {
        var normalizedName = tableName.ToLowerInvariant();
        
        if (!_tables.ContainsKey(normalizedName))
            return Task.FromResult(new QueryResult(false, null, 0, $"Table '{tableName}' does not exist"));

        var data = _tableData[normalizedName];
        var filteredRows = new List<IReadOnlyDictionary<string, object>>();

        foreach (var row in data)
        {
            if (MatchesConditions(row, plan.Conditions))
            {
                var filteredRow = FilterColumns(row, plan.Columns);
                filteredRows.Add(filteredRow);
            }
        }

        if (plan.OrderBy != null && plan.OrderBy.Count > 0)
        {
            filteredRows = ApplyOrdering(filteredRows, plan.OrderBy);
        }

        if (plan.Offset.HasValue && plan.Offset.Value > 0)
        {
            filteredRows = filteredRows.Skip(plan.Offset.Value).ToList();
        }

        if (plan.Limit.HasValue && plan.Limit.Value > 0)
        {
            filteredRows = filteredRows.Take(plan.Limit.Value).ToList();
        }

        return Task.FromResult(new QueryResult(true, filteredRows, filteredRows.Count, null));
    }

    public Task<QueryResult> ExecuteInsertAsync(string tableName, QueryPlan plan)
    {
        var normalizedName = tableName.ToLowerInvariant();
        
        if (!_tables.ContainsKey(normalizedName))
            return Task.FromResult(new QueryResult(false, null, 0, $"Table '{tableName}' does not exist"));

        var data = _tableData[normalizedName];
        var newRow = new Dictionary<string, object>();

        var nextId = data.Count + 1;
        newRow["id"] = nextId;

        if (plan.Parameters != null)
        {
            foreach (var parameter in plan.Parameters)
            {
                newRow[parameter.Key.ToLowerInvariant()] = parameter.Value;
            }
        }

        data.Add(newRow);

        return Task.FromResult(new QueryResult(true, null, 1, null));
    }

    public Task<QueryResult> ExecuteUpdateAsync(string tableName, QueryPlan plan)
    {
        var normalizedName = tableName.ToLowerInvariant();
        
        if (!_tables.ContainsKey(normalizedName))
            return Task.FromResult(new QueryResult(false, null, 0, $"Table '{tableName}' does not exist"));

        var data = _tableData[normalizedName];
        var updatedCount = 0;

        foreach (var row in data)
        {
            if (MatchesConditions(row, plan.Conditions))
            {
                if (plan.Parameters != null)
                {
                    foreach (var parameter in plan.Parameters)
                    {
                        row[parameter.Key.ToLowerInvariant()] = parameter.Value;
                    }
                }
                updatedCount++;
            }
        }

        return Task.FromResult(new QueryResult(true, null, updatedCount, null));
    }

    public Task<QueryResult> ExecuteDeleteAsync(string tableName, QueryPlan plan)
    {
        var normalizedName = tableName.ToLowerInvariant();
        
        if (!_tables.ContainsKey(normalizedName))
            return Task.FromResult(new QueryResult(false, null, 0, $"Table '{tableName}' does not exist"));

        var data = _tableData[normalizedName];
        var rowsToRemove = new List<Dictionary<string, object>>();

        foreach (var row in data)
        {
            if (MatchesConditions(row, plan.Conditions))
            {
                rowsToRemove.Add(row);
            }
        }

        foreach (var row in rowsToRemove)
        {
            data.Remove(row);
        }

        return Task.FromResult(new QueryResult(true, null, rowsToRemove.Count, null));
    }

    public IReadOnlyList<string> GetTableNames()
    {
        return _tables.Keys.ToArray();
    }

    private bool MatchesConditions(Dictionary<string, object> row, IReadOnlyList<QueryCondition>? conditions)
    {
        if (conditions == null || conditions.Count == 0)
            return true;

        foreach (var condition in conditions)
        {
            if (!row.TryGetValue(condition.Column.ToLowerInvariant(), out var value))
                return false;

            if (!EvaluateCondition(value, condition.Operator, condition.Value))
                return false;
        }

        return true;
    }

    private bool EvaluateCondition(object rowValue, ConditionOperator op, object conditionValue)
    {
        return op switch
        {
            ConditionOperator.Equal => Equals(rowValue, conditionValue),
            ConditionOperator.NotEqual => !Equals(rowValue, conditionValue),
            ConditionOperator.LessThan => Comparer<object>.Default.Compare(rowValue, conditionValue) < 0,
            ConditionOperator.LessThanOrEqual => Comparer<object>.Default.Compare(rowValue, conditionValue) <= 0,
            ConditionOperator.GreaterThan => Comparer<object>.Default.Compare(rowValue, conditionValue) > 0,
            ConditionOperator.GreaterThanOrEqual => Comparer<object>.Default.Compare(rowValue, conditionValue) >= 0,
            ConditionOperator.Like => rowValue.ToString()?.Contains(conditionValue.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase) ?? false,
            ConditionOperator.IsNull => rowValue == null,
            ConditionOperator.IsNotNull => rowValue != null,
            _ => false
        };
    }

    private IReadOnlyDictionary<string, object> FilterColumns(Dictionary<string, object> row, IReadOnlyList<string>? columns)
    {
        if (columns == null || columns.Contains("*"))
            return row;

        var filtered = new Dictionary<string, object>();
        foreach (var column in columns)
        {
            if (row.TryGetValue(column.ToLowerInvariant(), out var value))
                filtered[column] = value;
        }

        return filtered;
    }

    private List<IReadOnlyDictionary<string, object>> ApplyOrdering(List<IReadOnlyDictionary<string, object>> rows, IReadOnlyList<QueryOrder> orderBy)
    {
        IOrderedEnumerable<IReadOnlyDictionary<string, object>>? orderedRows = null;

        foreach (var order in orderBy)
        {
            if (orderedRows == null)
            {
                orderedRows = order.Direction == OrderDirection.Ascending
                    ? rows.OrderBy(r => r.TryGetValue(order.Column.ToLowerInvariant(), out var v) ? v : null)
                    : rows.OrderByDescending(r => r.TryGetValue(order.Column.ToLowerInvariant(), out var v) ? v : null);
            }
            else
            {
                orderedRows = order.Direction == OrderDirection.Ascending
                    ? orderedRows.ThenBy(r => r.TryGetValue(order.Column.ToLowerInvariant(), out var v) ? v : null)
                    : orderedRows.ThenByDescending(r => r.TryGetValue(order.Column.ToLowerInvariant(), out var v) ? v : null);
            }
        }

        return orderedRows?.ToList() ?? rows;
    }
}