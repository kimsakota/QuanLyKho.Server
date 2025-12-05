using System.ComponentModel.DataAnnotations;

namespace QuanLyKho.API.DTOs
{
    public class InventoryCheckDetailDto
    {
        [Required]
        public int ProductId { get; set; }

        public int SystemQty { get; set; } // Tồn kho trên phần mềm lúc kiểm (để lưu lịch sử đối chiếu)

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng thực tế không được âm")]
        public int ActualQty { get; set; } // Số lượng thực tế đếm được
    }

    public class CreateInventoryCheckRequest
    {
        public DateTime CheckDate { get; set; } = DateTime.Now;
        public string? Notes { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Danh sách kiểm kê không được trống")]
        public List<InventoryCheckDetailDto> Details { get; set; } = new List<InventoryCheckDetailDto>();
    }
}