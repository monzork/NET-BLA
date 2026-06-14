using System;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IRefreshTokenRepository
{
    Task SaveAsync(RefreshToken token);
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task RevokeAllForUserAsync(Guid userId);
}
