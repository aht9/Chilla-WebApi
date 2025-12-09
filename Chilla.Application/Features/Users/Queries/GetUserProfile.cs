using System.Data;
using System.Text.Json;
using Chilla.Application.Features.Users.Dtos;
using Chilla.Infrastructure.Persistence.Extensions;
using Chilla.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Chilla.Application.Features.Users.Queries;

public record GetUserProfileQuery(Guid UserId) : IRequest<UserProfileDto>;

public class GetUserProfileHandler : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly IDbConnection _dbConnection; // Dapper
    private readonly ICacheService _cacheService;    // Redis

    public GetUserProfileHandler(IDbConnection dbConnection, ICacheService cacheService)
    {
        _dbConnection = dbConnection;
        _cacheService = cacheService;
    }

    public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        string cacheKey = $"user:{request.UserId}";
        
        // 1. Try Redis
        var cachedUser = await _cacheService.GetAsync<UserProfileDto>(cacheKey, cancellationToken);
        if (cachedUser != null) return cachedUser;

        // 2. Query DB with Dapper (Fast, Read-Only)
        // Ignoring Soft Deleted users in SQL directly
        string sql = @"
            SELECT Id, FirstName, LastName, Username, Email, PhoneNumber 
            FROM Users 
            WHERE Id = @Id AND IsDeleted = 0";

        var userDto = await _dbConnection.QuerySingleOrDefaultAsync<UserProfileDto>(sql, new { Id = request.UserId }, cancellationToken: cancellationToken);

        if (userDto == null) throw new KeyNotFoundException("User not found");

        // 3. Set Cache
        await _cacheService.SetAsync(cacheKey, userDto, TimeSpan.FromMinutes(10), cancellationToken);

        return userDto;
    }
}