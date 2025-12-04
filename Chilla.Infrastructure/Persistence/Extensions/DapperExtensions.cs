using System.Data;
using System.Data.Common;
using Dapper;

namespace Chilla.Infrastructure.Persistence.Extensions;

public static class DapperExtensions
{
    // ----------------------------------------------------------------
    // 1. QuerySingleOrDefault (برای گرفتن یک رکورد یا Null)
    // ----------------------------------------------------------------
    public static async Task<T?> QuerySingleOrDefaultAsync<T>(
        this IDbConnection connection,
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        var command = new CommandDefinition(
            commandText: sql,
            parameters: param,
            transaction: transaction,
            commandTimeout: commandTimeout,
            cancellationToken: cancellationToken); // پاس دادن توکن کنسلیشن به Dapper

        return await connection.QuerySingleOrDefaultAsync<T>(command);
    }

    // ----------------------------------------------------------------
    // 2. QueryAsync (برای گرفتن لیست)
    // ----------------------------------------------------------------
    public static async Task<IEnumerable<T>> QueryAsync<T>(
        this IDbConnection connection,
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        var command = new CommandDefinition(
            commandText: sql,
            parameters: param,
            transaction: transaction,
            commandTimeout: commandTimeout,
            cancellationToken: cancellationToken);

        return await connection.QueryAsync<T>(command);
    }

    // ----------------------------------------------------------------
    // 3. ExecuteAsync (برای Insert, Update, Delete)
    // ----------------------------------------------------------------
    public static async Task<int> ExecuteAsync(
        this IDbConnection connection,
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        var command = new CommandDefinition(
            commandText: sql,
            parameters: param,
            transaction: transaction,
            commandTimeout: commandTimeout,
            cancellationToken: cancellationToken);

        return await connection.ExecuteAsync(command);
    }

    // ----------------------------------------------------------------
    // 4. QueryMultipleAsync (برای اجرای چند کوئری همزمان)
    // ----------------------------------------------------------------
    public static async Task<SqlMapper.GridReader> QueryMultipleAsync(
        this IDbConnection connection,
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureConnectionOpenAsync(connection, cancellationToken);

        var command = new CommandDefinition(
            commandText: sql,
            parameters: param,
            transaction: transaction,
            commandTimeout: commandTimeout,
            cancellationToken: cancellationToken);

        return await connection.QueryMultipleAsync(command);
    }

    // ----------------------------------------------------------------
    // Helper: مدیریت هوشمند کانکشن
    // ----------------------------------------------------------------
    private static async Task EnsureConnectionOpenAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        // اگر کانکشن بسته بود، بازش می‌کنیم.
        if (connection.State != ConnectionState.Open)
        {
            // تلاش برای باز کردن به صورت Async (اگر درایور پشتیبانی کند)
            if (connection is DbConnection dbConnection)
            {
                await dbConnection.OpenAsync(cancellationToken);
            }
            else
            {
                // فال‌بک به روش همگام برای درایورهای قدیمی
                connection.Open();
            }
        }
    }
}