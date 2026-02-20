using System.Data;
using Dapper;

namespace Chilla.Domain.Common;

public interface IDapperService
{
    Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? param = null, CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default);
    
    Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default);
    
    Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param = null, CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default);
    
    Task<int> ExecuteAsync(string sql, object? param = null, CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default);
    
    Task<T?> ExecuteScalarAsync<T>(string sql, object? param = null, CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default);

    // برای کوئری‌های پیچیده که چند نتیجه برمی‌گردانند (مثل داشبوردها)
    Task<SqlMapper.GridReader> QueryMultipleAsync(string sql, object? param = null, CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default);
}