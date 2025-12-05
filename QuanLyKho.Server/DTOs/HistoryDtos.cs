using System;
using System.Collections.Generic;

namespace QuanLyKho.API.DTOs
{
    // DTO cho danh sách lịch sử (LoadDataAsync)
    public class HistoryItemDto
    {
        public int Id { get; set; }
        public string TransactionCode { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "Nhập kho", "Xuất kho", "Kiểm kê"
        public DateTime Date { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public decimal? TotalAmount { get; set; }
        public string Creator { get; set; } = string.Empty;
    }

    // DTO cho chi tiết giao dịch (ViewDetails)
    public class TransactionDetailDto
    {
        public string TransactionCode { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string PartnerName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Creator { get; set; } = string.Empty;
        public decimal? TotalAmount { get; set; }

        public List<HistoryDetailItemDto> Details { get; set; } = new();
        public List<InventoryDetailItemDto> InventoryDetails { get; set; } = new();
    }

    public class HistoryDetailItemDto
    {
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
    }

    public class InventoryDetailItemDto
    {
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public int SystemQty { get; set; }
        public int ActualQty { get; set; }
        public int Diff => ActualQty - SystemQty;
    }
}