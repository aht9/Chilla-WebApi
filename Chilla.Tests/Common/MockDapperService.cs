using System.Data;
using Chilla.Domain.Common;
using Dapper;

namespace Chilla.Tests.Common;

public class MockDapperService : IDapperService
{
    private readonly Dictionary<string, object> _mockData = new();

    public void SetupMockData<T>(string sql, T data)
    {
        _mockData[sql] = data;
    }

    public void SetupMockDataList<T>(string sql, List<T> data)
    {
        _mockData[sql] = data;
    }

    public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? param = null, CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default)
    {
        if (_mockData.TryGetValue(sql, out var data) && data is List<T> list)
        {
            return list.AsReadOnly();
        }
        return new List<T>().AsReadOnly();
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default)
    {
        if (_mockData.TryGetValue(sql, out var data))
        {
            return data is T ? (T)data : default;
        }
        return default;
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param = null, CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default)
    {
        if (_mockData.TryGetValue(sql, out var data))
        {
            return data is T ? (T)data : default;
        }
        return default;
    }

    public async Task<int> ExecuteAsync(string sql, object? param = null, CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default)
    {
        // Mock execute operation - return 1 to indicate success
        return 1;
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql, object? param = null, CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default)
    {
        // Mock scalar operation
        if (typeof(T) == typeof(int))
            return (T)(object)1;
        return default;
    }

    public async Task<SqlMapper.GridReader> QueryMultipleAsync(string sql, object? param = null, CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default)
    {
        // For simplicity, return an empty grid reader
        // In real tests, you might want to create a mock grid reader
        throw new NotImplementedException("Mock QueryMultipleAsync not implemented for simplicity");
    }
}
