using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyKho.API.DTOs
{
    // Class nhận dữ liệu từ Client gửi lên
    public class CreateImportRequest
    {
        [Required]
        public int SupplierId { get; set; }

        public DateTime ImportDate { get; set; } = DateTime.Now;

        public string? ImportedBy { get; set; }

        [Required]
        public List<ImportItemDto> Details { get; set; } = new();
    }

    public class ImportItemDto
    {
        public int ProductId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá không hợp lệ")]
        public decimal UnitPrice { get; set; }
    }
}