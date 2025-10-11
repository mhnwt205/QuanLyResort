using System.ComponentModel.DataAnnotations;

namespace QuanLyResort.ViewModels
{
    public class InvoiceDto
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int? BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public List<InvoiceItemViewModel> InvoiceItems { get; set; } = new();
        public List<PaymentViewModel> Payments { get; set; } = new();
    }

    public class CreateInvoiceDto
    {
        [Required(ErrorMessage = "Vui lòng chọn khách hàng")]
        public int CustomerId { get; set; }

        public int? BookingId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày hóa đơn")]
        public DateTime InvoiceDate { get; set; }

        public DateTime? DueDate { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Tổng tiền trước thuế phải lớn hơn 0")]
        public decimal Subtotal { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Thuế VAT không được âm")]
        public decimal TaxAmount { get; set; } = 0;

        [Range(0, double.MaxValue, ErrorMessage = "Giảm giá không được âm")]
        public decimal DiscountAmount { get; set; } = 0;

        [Range(0, double.MaxValue, ErrorMessage = "Tổng tiền phải lớn hơn 0")]
        public decimal TotalAmount { get; set; }

        public string? PaymentMethod { get; set; }
        public string? Notes { get; set; }
    }

    public class InvoiceItemViewModel
    {
        public int ItemId { get; set; }
        public int InvoiceId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PaymentViewModel
    {
        public int PaymentId { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public int InvoiceId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string ProcessedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreatePaymentDto
    {
        [Required(ErrorMessage = "Vui lòng chọn hóa đơn")]
        public int InvoiceId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày thanh toán")]
        public DateTime PaymentDate { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        public string PaymentMethod { get; set; } = string.Empty;

        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
    }
}
