using QuanLyKho.Server.DTOs;

namespace QuanLyKho.Server.Services
{
    public interface IAuthService
    {
        Task<AuthResponse?> LoginAsync(LoginRequest request);
        Task<bool> ValidateTokenAsync(string token);
    }
}
