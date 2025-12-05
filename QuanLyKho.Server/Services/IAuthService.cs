using QuanLyKho.API.DTOs;

namespace QuanLyKho.API.Services
{
    public interface IAuthService
    {
        Task<AuthResponse?> LoginAsync(LoginRequest request);
        Task<bool> ValidateTokenAsync(string token);
    }
}
