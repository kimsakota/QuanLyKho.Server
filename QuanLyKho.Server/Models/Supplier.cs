using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyKho.Server.Models
{
    public partial class Supplier : ObservableValidator
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên nhà cung cấp là bắt buộc")]
        [ObservableProperty]
        private string? name; // Tên công ty/nhà cung cấp

        [ObservableProperty]
        private string? contactPerson; // Tên người liên hệ

        [ObservableProperty]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        private string? phoneNumber;

        [ObservableProperty]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        private string? email;

        [ObservableProperty]
        private string? address; // Địa chỉ

        [ObservableProperty]
        private string? taxCode; // Mã số thuế

        [ObservableProperty]
        private string? bankName; //Tên ngân hàng

        [ObservableProperty]
        private string? accountName; //Chủ tài khoản

        [ObservableProperty]
        private string? accountNumber; 

        [ObservableProperty]
        private string? notes; // Ghi chú thêm

        public ICollection<Import> Imports { get; set; } = new List<Import>();

        public void ValidateAll() => base.ValidateAllProperties();
    }
}
