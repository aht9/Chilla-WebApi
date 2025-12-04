using System.Data;
using System.Text.Json;
using Chilla.Application.Features.Users.Dtos;
using Chilla.Infrastructure.Persistence.Extensions;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Chilla.Application.Features.Users.Queries;

public record GetUserProfileQuery(Guid UserId) : IRequest<UserProfileDto>;

public class GetUserProfileHandler : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly IDbConnection _dbConnection; // Dapper
    private readonly IDistributedCache _cache;    // Redis

    public GetUserProfileHandler(IDbConnection dbConnection, IDistributedCache cache)
    {
        _dbConnection = dbConnection;
        _cache = cache;
    }

    public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        string cacheKey = $"user:{request.UserId}";
        
        // 1. Try Redis
        var cachedData = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedData))
        {
            return JsonSerializer.Deserialize<UserProfileDto>(cachedData);
        }

        // 2. Query DB with Dapper (Fast, Read-Only)
        // Ignoring Soft Deleted users in SQL directly
        string sql = @"
            SELECT Id, FirstName, LastName, Username, Email, PhoneNumber 
            FROM Users 
            WHERE Id = @Id AND IsDeleted = 0";

        var userDto = await _dbConnection.QuerySingleOrDefaultAsync<UserProfileDto>(sql, new { Id = request.UserId });

        if (userDto == null) throw new KeyNotFoundException("User not found");

        // 3. Set Cache
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(userDto), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        return userDto;
    }
}