using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
namespace QuanLyKho.API.Models
{
    public partial class Product : ObservableValidator
    {
        [Key]
        public int Id { get; set; }

        [ObservableProperty]
        private string? imagePath = "pack://application:,,,/Assets/Images/logo-image.png";

        [Required(ErrorMessage = "Mã sản phẩm là bắt buộc")]
        [ObservableProperty]
        private string? productCode;

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [ObservableProperty]
        private string? productName;

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng ban đầu không hợp lệ.")]
        [ObservableProperty]
        private int initialQty;

        //[Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Giá vốn không hợp lệ.")]
        //public decimal CostPrice { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Giá bán không hợp lệ.")]
        public decimal SalePrice { get; set; }

        [ObservableProperty]
        private DateTime? expiryDate = null;

        [ObservableProperty]
        private string? description;

        [ObservableProperty]
        private int? categoryId;

        [ForeignKey(nameof(CategoryId))]
        public Category? Category { get; set; }

        [NotMapped]
        public BitmapImage? Image { get; set; }

        [property: NotMapped]
        [ObservableProperty]
        private bool isSelected = false;

        public ICollection<ImportDetail> ImportDetails { get; set; } = new List<ImportDetail>();
        public ICollection<ExportDetail> ExportDetails { get; set; } = new List<ExportDetail>();

        public void ValidateAll() => base.ValidateAllProperties();

    }
}