namespace QuanLyKho.Server.DTOs
{
    // Dữ liệu tổng hợp cho báo cáo tồn kho
    public class InventoryReportResponse
    {
        public int TotalProductsCount { get; set; }
        public int TotalStockQuantity { get; set; }
        public decimal TotalStockValue { get; set; }
        public int LowStockCount { get; set; }

        public List<LowStockProductDto> LowStockProducts { get; set; } = new();
        public List<ChartItemDto> CategoryValueChart { get; set; } = new();
        public List<ChartItemDto> TopValueProductChart { get; set; } = new();
    }

    // Sản phẩm sắp hết hàng
    public class LowStockProductDto
    {
        public int Id { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int InitialQty { get; set; }
    }

    // Dữ liệu vẽ biểu đồ
    public class ChartItemDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }

    public class SupplierReportResponse
    {
        public int TotalSuppliers { get; set; }       // Tổng số NCC
        public int ActiveSuppliers { get; set; }      // Số NCC hoạt động trong kỳ
        public int TotalImportOrders { get; set; }    // Tổng số phiếu nhập
        public decimal TotalImportCost { get; set; }  // Tổng chi phí nhập
        public List<TopSupplierDto> TopSuppliers { get; set; } = new();
    }

    public class TopSupplierDto
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int OrderCount { get; set; }           // Số lần nhập
        public decimal TotalImportValue { get; set; } // Tổng giá trị nhập
    }
}