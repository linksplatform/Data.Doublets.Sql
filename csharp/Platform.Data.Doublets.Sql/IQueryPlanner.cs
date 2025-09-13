using System;
using System.Collections.Generic;
using System.Linq;
using SqlParser.Ast;

namespace Platform.Data.Doublets.Sql;

public interface IQueryPlanner
{
    QueryPlan CreatePlan(Statement statement);
}

public record QueryPlan(
    QueryType Type,
    string? TableName,
    IReadOnlyList<string>? Columns,
    IReadOnlyDictionary<string, object>? Parameters,
    IReadOnlyList<QueryCondition>? Conditions,
    IReadOnlyList<QueryOrder>? OrderBy,
    int? Limit,
    int? Offset
);

public enum QueryType
{
    Select,
    Insert,
    Update,
    Delete,
    CreateTable,
    DropTable,
    AlterTable
}

public record QueryCondition(
    string Column,
    ConditionOperator Operator,
    object Value
);

public enum ConditionOperator
{
    Equal,
    NotEqual,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    Like,
    In,
    NotIn,
    IsNull,
    IsNotNull
}

public record QueryOrder(
    string Column,
    OrderDirection Direction
);

public enum OrderDirection
{
    Ascending,
    Descending
}

public class QueryPlannerImplementation : IQueryPlanner
{
    public QueryPlan CreatePlan(Statement statement)
    {
        return statement switch
        {
            Statement.Query query => CreateQueryPlan(query),
            Statement.Insert insert => CreateInsertPlan(insert),
            Statement.Update update => CreateUpdatePlan(update),
            Statement.Delete delete => CreateDeletePlan(delete),
            Statement.CreateTable createTable => CreateCreateTablePlan(createTable),
            Statement.Drop drop => CreateDropPlan(drop),
            _ => throw new NotSupportedException($"Statement type {statement.GetType().Name} is not supported")
        };
    }

    private QueryPlan CreateQueryPlan(Statement.Query query)
    {
        var body = query.Body;
        if (body is SetExpression.Select select)
        {
            var tableName = ExtractTableName(select.From);
            var columns = ExtractColumns(select.Projection);
            var conditions = ExtractConditions(select.Selection);
            var orderBy = ExtractOrderBy(query.OrderBy);
            var limit = ExtractLimit(query.Limit);
            var offset = ExtractOffset(query.Offset);

            return new QueryPlan(
                QueryType.Select,
                tableName,
                columns,
                null,
                conditions,
                orderBy,
                limit,
                offset
            );
        }

        throw new NotSupportedException("Only SELECT queries are currently supported in query statements");
    }

    private QueryPlan CreateInsertPlan(Statement.Insert insert)
    {
        var tableName = insert.TableName.ToString();
        var columns = insert.Columns?.Select(c => c.Value).ToArray();
        
        return new QueryPlan(
            QueryType.Insert,
            tableName,
            columns,
            null,
            null,
            null,
            null,
            null
        );
    }

    private QueryPlan CreateUpdatePlan(Statement.Update update)
    {
        var tableName = update.Table.ToString();
        var conditions = ExtractConditions(update.Selection);

        return new QueryPlan(
            QueryType.Update,
            tableName,
            null,
            null,
            conditions,
            null,
            null,
            null
        );
    }

    private QueryPlan CreateDeletePlan(Statement.Delete delete)
    {
        var tableName = ExtractTableName(delete.From);
        var conditions = ExtractConditions(delete.Selection);

        return new QueryPlan(
            QueryType.Delete,
            tableName,
            null,
            null,
            conditions,
            null,
            null,
            null
        );
    }

    private QueryPlan CreateCreateTablePlan(Statement.CreateTable createTable)
    {
        var tableName = createTable.Name.ToString();

        return new QueryPlan(
            QueryType.CreateTable,
            tableName,
            null,
            null,
            null,
            null,
            null,
            null
        );
    }

    private QueryPlan CreateDropPlan(Statement.Drop drop)
    {
        if (drop.ObjectType == ObjectType.Table && drop.Names.Count > 0)
        {
            var tableName = drop.Names[0].ToString();
            return new QueryPlan(
                QueryType.DropTable,
                tableName,
                null,
                null,
                null,
                null,
                null,
                null
            );
        }

        throw new NotSupportedException("Only DROP TABLE statements are supported");
    }

    private string? ExtractTableName(IReadOnlyList<TableWithJoins>? from)
    {
        if (from == null || from.Count == 0)
            return null;

        var tableWithJoins = from[0];
        return tableWithJoins.Relation switch
        {
            TableFactor.Table table => table.Name.ToString(),
            _ => null
        };
    }

    private IReadOnlyList<string>? ExtractColumns(IReadOnlyList<SelectItem> projection)
    {
        var columns = new List<string>();
        
        foreach (var item in projection)
        {
            if (item is SelectItem.UnnamedExpression unnamed)
            {
                if (unnamed.Expression is Expression.Identifier identifier)
                {
                    columns.Add(identifier.Ident.Value);
                }
                else if (unnamed.Expression is Expression.Wildcard)
                {
                    columns.Add("*");
                }
            }
            else if (item is SelectItem.ExpressionWithAlias aliased)
            {
                columns.Add(aliased.Alias.Value);
            }
        }

        return columns.Count > 0 ? columns : null;
    }

    private IReadOnlyList<QueryCondition>? ExtractConditions(Expression? selection)
    {
        if (selection == null)
            return null;

        var conditions = new List<QueryCondition>();
        
        return conditions.Count > 0 ? conditions : null;
    }

    private IReadOnlyList<QueryOrder>? ExtractOrderBy(IReadOnlyList<OrderByExpression>? orderBy)
    {
        if (orderBy == null || orderBy.Count == 0)
            return null;

        var orders = new List<QueryOrder>();
        
        foreach (var order in orderBy)
        {
            if (order.Expression is Expression.Identifier identifier)
            {
                var direction = order.Asc == true ? OrderDirection.Ascending : OrderDirection.Descending;
                orders.Add(new QueryOrder(identifier.Ident.Value, direction));
            }
        }

        return orders.Count > 0 ? orders : null;
    }

    private int? ExtractLimit(Expression? limit)
    {
        if (limit is Expression.LiteralValue literal && literal.Value is Value.Number number)
        {
            if (int.TryParse(number.Value, out var limitValue))
                return limitValue;
        }
        
        return null;
    }

    private int? ExtractOffset(Offset? offset)
    {
        if (offset?.Value is Expression.LiteralValue literal && literal.Value is Value.Number number)
        {
            if (int.TryParse(number.Value, out var offsetValue))
                return offsetValue;
        }
        
        return null;
    }
}