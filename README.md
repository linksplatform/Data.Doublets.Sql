# Data.Doublets.Sql

A comprehensive SQL interface for Doublets data storage, providing full SQL server functionality with support for both the native Doublets links table and custom virtual tables.

## Features

- **Full SQL Support**: Complete implementation of SQL standard operations (SELECT, INSERT, UPDATE, DELETE, CREATE TABLE, DROP TABLE)
- **Doublets Integration**: Direct access to the Doublets links storage through SQL queries
- **Virtual Tables**: Support for creating and managing custom virtual tables alongside the native links table
- **REST API**: HTTP/JSON API for executing SQL queries programmatically
- **Standard SQL Compliance**: Uses industry-standard SQL parsing for broad compatibility

## Quick Start

### Prerequisites

- .NET 8.0 or later
- Platform.Data.Doublets package

### Running the SQL Server

```bash
cd csharp/Platform.Data.Doublets.Sql.Server
dotnet run
```

The server will start on `http://localhost:1433` by default. You can specify a custom port and database file:

```bash
dotnet run -- mydb.links 5432
```

### API Documentation

Once running, visit `http://localhost:1433/swagger` for interactive API documentation.

## SQL Examples

### Working with Links Table

The `links` table provides direct access to Doublets storage with columns: `id`, `source`, `target`.

```sql
-- View all links
SELECT * FROM links;

-- Find specific link by ID
SELECT * FROM links WHERE id = 1;

-- Find links by source
SELECT * FROM links WHERE source = 2;

-- Find links by target  
SELECT * FROM links WHERE target = 3;

-- Delete a link
DELETE FROM links WHERE id = 1;
```

### Working with Virtual Tables

```sql
-- Create a custom table
CREATE TABLE users (id INTEGER, name TEXT, email TEXT);

-- Insert data
INSERT INTO users (name, email) VALUES ('John Doe', 'john@example.com');

-- Query data
SELECT * FROM users WHERE name LIKE '%John%';

-- Update data
UPDATE users SET email = 'newemail@example.com' WHERE id = 1;

-- Drop table
DROP TABLE users;
```

## REST API Usage

### Execute SQL Query

```bash
curl -X POST http://localhost:1433/api/sql/execute \
  -H "Content-Type: application/json" \
  -d '{"sql": "SELECT * FROM links LIMIT 10"}'
```

### List Tables

```bash
curl http://localhost:1433/api/sql/tables
```

### Health Check

```bash
curl http://localhost:1433/api/sql/health
```

## Architecture

The system consists of several key components:

- **SQL Parser**: Parses SQL statements using the SqlParserCS library
- **Query Planner**: Converts parsed SQL into execution plans
- **Doublets Query Executor**: Executes plans against Doublets storage and virtual tables
- **Virtual Table Manager**: Manages custom table schemas and data
- **REST API**: Provides HTTP endpoints for SQL execution

## Development

### Building

```bash
cd csharp
dotnet build
```

### Running Tests

```bash
cd csharp
dotnet test
```

### Project Structure

- `Platform.Data.Doublets.Sql/` - Core SQL implementation
- `Platform.Data.Doublets.Sql.Server/` - HTTP server and REST API
- `Platform.Data.Doublets.Sql.Tests/` - Comprehensive test suite

## Supported SQL Features

### Query Types
- ✅ SELECT with WHERE, ORDER BY, LIMIT, OFFSET
- ✅ INSERT with VALUES
- ✅ UPDATE with WHERE conditions
- ✅ DELETE with WHERE conditions
- ✅ CREATE TABLE
- ✅ DROP TABLE

### Data Types
- INTEGER
- TEXT
- REAL
- BLOB
- BOOLEAN

### Operators
- Comparison: =, <>, <, <=, >, >=
- Pattern matching: LIKE
- Null checks: IS NULL, IS NOT NULL

## License

This project is released under the [Unlicense](LICENSE).
