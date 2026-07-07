using AutoMapper;
using UserService.Application.Common;
using UserService.Application.DTO;
using UserService.Application.Interfaces;
using UserService.Application.RequestResponse;
using UserService.Domain.Entity;

namespace UserService.Application.Services
{
    public class UserServiceImpl : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        private readonly IMapper _mapper;

        public UserServiceImpl(
            IUserRepository userRepository,
            IJwtService jwtService,
            IMapper mapper)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _mapper = mapper;
        }

        public async Task<UserDto> RegisterAsync(RegisterRequest request)
        {
            var exists = await _userRepository.ExistsAsync(request.Email);
            if (exists)
                throw new UserAlreadyExistsException(request.Email);

            var user = _mapper.Map<User>(request);
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            await _userRepository.AddAsync(user);
            return _mapper.Map<UserDto>(user);
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new InvalidCredentialsException();

            var token = _jwtService.GenerateToken(user);
            var expiry = DateTime.UtcNow.AddMinutes(60);

            return new LoginResponseDto
            {
                Token = token,
                Expiry = expiry,
                User = _mapper.Map<UserDto>(user)
            };
        }

        public async Task<UserDto?> GetByIdAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user is null)
                throw new UserNotFoundException(id.ToString());

            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto?> UpdateAsync(UpdateUserRequest request)
        {
            var user = await _userRepository.GetByIdAsync(request.Id);
            if (user is null)
                throw new UserNotFoundException(request.Id.ToString());

            _mapper.Map(request, user);
            await _userRepository.UpdateAsync(user);
            return _mapper.Map<UserDto>(user);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user is null)
                throw new UserNotFoundException(id.ToString());

            await _userRepository.DeleteAsync(user);
            return true;
        }
    }
}
