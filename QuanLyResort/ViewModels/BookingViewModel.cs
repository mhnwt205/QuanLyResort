using System.ComponentModel.DataAnnotations;

namespace QuanLyResort.ViewModels
{
    public class BookingViewModel
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int? RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int Adults { get; set; }
        public int Children { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal? DepositAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string SpecialRequests { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class CreateBookingDto
    {
        [Required(ErrorMessage = "Vui lòng chọn khách hàng")]
        public int CustomerId { get; set; }

        public int? RoomId { get; set; } // nullable -> chọn sau

        [Required(ErrorMessage = "Vui lòng chọn ngày nhận phòng")]
        public DateTime CheckInDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày trả phòng")]
        public DateTime CheckOutDate { get; set; }

        [Range(1, 10, ErrorMessage = "Số người lớn phải từ 1 đến 10")]
        public int Adults { get; set; } = 1;

        [Range(0, 10, ErrorMessage = "Số trẻ em phải từ 0 đến 10")]
        public int Children { get; set; } = 0;

        public bool ChargeToRoom { get; set; } = true;

        public string? SpecialRequests { get; set; }
    }

    public class BookingDetailsViewModel : BookingViewModel
    {
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public decimal RoomPrice { get; set; }
        public int Nights { get; set; }
        public List<ServiceBookingViewModel> ServiceBookings { get; set; } = new();
        public List<InvoiceViewModel> Invoices { get; set; } = new();
        public List<CheckInViewModel> CheckIns { get; set; } = new();
    }

    public class ServiceBookingViewModel
    {
        public int ServiceBookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
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

    public class InvoiceViewModel
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
    }

    public class CheckInViewModel
    {
        public int CheckInId { get; set; }
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public string CheckedInBy { get; set; } = string.Empty;
        public string CheckedOutBy { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}
