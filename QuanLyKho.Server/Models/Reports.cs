namespace QuanLyKho.Server.Models
{
    public class Reports
    {
    }

    public class FinancialReportResponse
    {
        public decimal TotalRevenue { get; set; } // Tổng thu
        public decimal TotalCost { get; set; }    // Tổng chi
        public decimal TotalProfit { get; set; }  // Lợi nhuận
        public List<DailyFinancialStats> DailyStats { get; set; } = new();
    }

    public class DailyFinancialStats
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Profit => Revenue - Cost;
    }

    public class CustomerReportReponse
    {
        public int TotalCustomers { get; set; }       // Tổng khách hàng trong DB
        public int ActiveCustomers { get; set; }      // Khách có mua hàng trong thời gian
        public int TotalOrders { get; set; }          // Tổng đơn hàng trong thời gian
        public decimal TotalRevenue { get; set; }     // Doanh thu trong thời gian
        public List<TopCustomerDto> TopCustomers { get; set; } = new(); // Danh sách khách hàng chi tiêu nhiều nhất
    }

    public class TopCustomerDto
    {
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
    }
}
