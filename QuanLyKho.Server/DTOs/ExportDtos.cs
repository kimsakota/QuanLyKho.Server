using System.ComponentModel.DataAnnotations;

namespace QuanLyKho.API.DTOs
{
    /// <summary>
    /// DTO chi tiết từng sản phẩm trong phiếu xuất
    /// </summary>
    public class ExportItemDto
    {
        [Required(ErrorMessage = "Mã sản phẩm là bắt buộc")]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng xuất phải lớn hơn 0")]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá không được âm")]
        public decimal UnitPrice { get; set; }
    }

    /// <summary>
    /// DTO yêu cầu tạo phiếu xuất gửi từ Client
    /// </summary>
    public class CreateExportRequest
    {
        // ID khách hàng đã chọn (nếu có)
        public int CustomerId { get; set; }

        // --- Các trường dùng để tạo nhanh khách hàng mới (nếu CustomerId = 0) ---
        public string? NewCustomerName { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? NewCustomerPhone { get; set; }

        public string? NewCustomerAddress { get; set; }
        // ------------------------------------------------------------------------

        [Required(ErrorMessage = "Danh sách sản phẩm không được để trống")]
        [MinLength(1, ErrorMessage = "Phải có ít nhất một sản phẩm để xuất kho")]
        public List<ExportItemDto> Details { get; set; } = new List<ExportItemDto>();
    }
}