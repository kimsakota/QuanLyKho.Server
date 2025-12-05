namespace QuanLyKho.API.DTOs
{
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string? Username { get; set; } 
        public string? FullName { get; set; }
        public string? Role { get; set; }
    }
}
