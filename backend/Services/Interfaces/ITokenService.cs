using SmartFleet.Backend.Models;

namespace SmartFleet.Backend.Services.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}
