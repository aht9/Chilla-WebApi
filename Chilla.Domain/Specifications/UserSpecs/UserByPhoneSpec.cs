using Chilla.Domain.Aggregates.UserAggregate;

namespace Chilla.Domain.Specifications.UserSpecs;

public class UserByPhoneSpec : BaseSpecification<User>
{
    public UserByPhoneSpec(string phoneNumber) 
        : base(u => u.PhoneNumber == phoneNumber && !u.IsDeleted)
    {
    }
}

// Chilla.Domain/Specifications/UserSpecs/UserByRefreshTokenSpec.cs
public class UserByRefreshTokenSpec : BaseSpecification<User>
{
    public UserByRefreshTokenSpec(string token) 
        : base(u => u.RefreshTokens.Any(t => t.Token == token) && !u.IsDeleted)
    {
        AddInclude(u => u.RefreshTokens);
    }
}