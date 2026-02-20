using Chilla.Application.Features.Users.Dtos;
using Chilla.Domain.Common;
using MediatR;

namespace Chilla.Application.Features.Users.Queries;

public record GetUserProfileQuery(Guid UserId) : IRequest<UserProfileDto>;

public class GetUserProfileHandler : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly IDapperService _dapperService; // استفاده از سرویس Dapper جدید
    private readonly ICacheService _cacheService;   // Redis

    public GetUserProfileHandler(IDapperService dapperService, ICacheService cacheService)
    {
        _dapperService = dapperService;
        _cacheService = cacheService;
    }

    public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        string cacheKey = $"user:{request.UserId}";
        
        // 1. Try Redis
        var cachedUser = await _cacheService.GetAsync<UserProfileDto>(cacheKey, cancellationToken);
        if (cachedUser != null) return cachedUser;

        // 2. Query DB with Dapper Service (Fast, Read-Only)
        // Ignoring Soft Deleted users in SQL directly
        string sql = @"
            SELECT Id, FirstName, LastName, Username, Email, PhoneNumber 
            FROM Users 
            WHERE Id = @Id AND IsDeleted = 0";

        // فراخوانی متد از طریق IDapperService
        var userDto = await _dapperService.QuerySingleOrDefaultAsync<UserProfileDto>(
            sql, 
            new { Id = request.UserId }, 
            cancellationToken: cancellationToken);

        if (userDto == null) throw new KeyNotFoundException("User not found");

        // 3. Set Cache
        await _cacheService.SetAsync(cacheKey, userDto, TimeSpan.FromMinutes(10), null,cancellationToken);

        return userDto;
    }
}