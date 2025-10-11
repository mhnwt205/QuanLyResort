using System.ComponentModel.DataAnnotations;

namespace QuanLyResort.ViewModels
{
    public class ServiceViewModel
    {
        public int ServiceId { get; set; }
        public string ServiceCode { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public string Unit { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ServiceBookingDto
    {
        public int ServiceBookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public DateTime ServiceDate { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string SpecialRequests { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class CreateServiceBookingDto
    {
        [Required(ErrorMessage = "Vui lòng chọn khách hàng")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn dịch vụ")]
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày đặt")]
        public DateTime BookingDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày sử dụng dịch vụ")]
        public DateTime ServiceDate { get; set; }

        [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 đến 100")]
        public int Quantity { get; set; } = 1;

        public string? SpecialRequests { get; set; }
    }

    public class ServiceCategoryViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int ServiceCount { get; set; }
    }
}
