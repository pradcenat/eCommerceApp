using UserService.Domain.Entity;

namespace UserService.Application.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }
}
