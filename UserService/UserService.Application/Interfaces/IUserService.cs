using UserService.Application.DTO;
using UserService.Application.RequestResponse;

namespace UserService.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserDto> RegisterAsync(RegisterRequest request);
        Task<LoginResponseDto> LoginAsync(LoginRequest request);
        Task<UserDto?> GetByIdAsync(Guid id);
        Task<UserDto?> UpdateAsync(UpdateUserRequest request);
        Task<bool> DeleteAsync(Guid id);
    }
}
