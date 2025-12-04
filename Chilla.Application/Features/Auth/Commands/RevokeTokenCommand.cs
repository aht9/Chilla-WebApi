using Chilla.Domain.Common;
using Chilla.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Chilla.Application.Features.Auth.Commands;

public record RevokeTokenCommand(string Token, string IpAddress) : IRequest<bool>;

// Handler implementation
public class RevokeTokenHandler : IRequestHandler<RevokeTokenCommand, bool>
{
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeTokenHandler(AppDbContext context, IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Token)) return false;

        // جستجوی کاربری که این توکن را دارد
        var user = await _context.Users
            .Include(u => u.RefreshTokens)
            .SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == request.Token), cancellationToken);

        // اگر کاربر پیدا نشد یا توکن وجود نداشت، عملاً کار خاصی نمی‌توان کرد (Idempotent)
        if (user == null) return false;

        // ابطال توکن
        var result = user.RevokeRefreshToken(request.Token, request.IpAddress, "Manual Logout");

        if (result)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return result;
    }
}