using System.Data;
using Chilla.Domain.Common;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Chilla.Infrastructure.Persistence.Services;

public class DapperService : IDapperService
{
    private readonly string _connectionString;

    public DapperService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new ArgumentNullException(nameof(configuration), "Connection string not found.");
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? param = null, CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var command = new CommandDefinition(sql, param, commandType: commandType, cancellationToken: cancellationToken);
        var result = await connection.QueryAsync<T>(command);
        return result.ToList().AsReadOnly();
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var command = new CommandDefinition(sql, param, commandType: commandType, cancellationToken: cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<T>(command);
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param = null, CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var command = new CommandDefinition(sql, param, commandType: commandType, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<T>(command);
    }

    public async Task<int> ExecuteAsync(string sql, object? param = null, CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var command = new CommandDefinition(sql, param, commandType: commandType, cancellationToken: cancellationToken);
        return await connection.ExecuteAsync(command);
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql, object? param = null, CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var command = new CommandDefinition(sql, param, commandType: commandType, cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<T>(command);
    }

    public async Task<SqlMapper.GridReader> QueryMultipleAsync(string sql, object? param = null, CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default)
    {
        // نکته: خریدار این متد (فراخواننده) باید Connection/GridReader را Dispose کند.
        // بنابراین در اینجا از using استفاده نمی‌کنیم.
        var connection = CreateConnection();
        var command = new CommandDefinition(sql, param, commandType: commandType, cancellationToken: cancellationToken);
        return await connection.QueryMultipleAsync(command);
    }
}