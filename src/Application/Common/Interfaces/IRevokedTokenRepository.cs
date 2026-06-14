using System;
using System.Threading.Tasks;

namespace Application.Common.Interfaces;

public interface IRevokedTokenRepository
{
    Task RevokeAsync(string token, DateTime expiryDate);
    Task<bool> IsRevokedAsync(string token);
}
