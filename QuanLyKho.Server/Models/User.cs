using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyKho.Server.Models
{
    public partial class User : ObservableValidator
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên người dùng là bắt buộc")]
        [MaxLength(100)]
        [ObservableProperty]
        private string? fullName;

        [MaxLength(20)]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [ObservableProperty]
        private string? phoneNumber;

        [MaxLength(200)]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [ObservableProperty] 
        private string? email;

        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [MaxLength(100)]
        [ObservableProperty] 
        private string? username;

        public string? PasswordHash { get; set; }

        [Required]
        [MaxLength(50)]
        [ObservableProperty] 
        private string role = "Employee";

        public void ValidateAll() => base.ValidateAllProperties();
    }
}
