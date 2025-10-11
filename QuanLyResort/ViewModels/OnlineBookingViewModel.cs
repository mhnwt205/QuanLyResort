using System.ComponentModel.DataAnnotations;
using QuanLyResort.Models;

namespace QuanLyResort.ViewModels
{
    public class OnlineBookingViewModel
    {
        // Thông tin phòng
        public int RoomId { get; set; }
        public string RoomName { get; set; } = "";
        public string RoomType { get; set; } = "";
        public decimal RoomPrice { get; set; }
        public string RoomImage { get; set; } = "";
        public int MaxOccupancy { get; set; } = 4; // Sức chứa tối đa của phòng

        // Thông tin đặt phòng
        [Required(ErrorMessage = "Ngày nhận phòng là bắt buộc")]
        [Display(Name = "Ngày nhận phòng")]
        public DateTime CheckInDate { get; set; } = DateTime.Today.AddDays(1);

        [Required(ErrorMessage = "Ngày trả phòng là bắt buộc")]
        [Display(Name = "Ngày trả phòng")]
        public DateTime CheckOutDate { get; set; } = DateTime.Today.AddDays(2);

        [Required(ErrorMessage = "Số lượng khách là bắt buộc")]
        [Range(1, 10, ErrorMessage = "Số lượng khách phải từ 1 đến 10")]
        [Display(Name = "Số lượng khách")]
        public int GuestCount { get; set; } = 1;

        // Thông tin khách hàng
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên phải có từ 2 đến 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string CustomerName { get; set; } = "";

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được quá 100 ký tự")]
        [Display(Name = "Email")]
        public string CustomerEmail { get; set; } = "";

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [RegularExpression(@"^[0-9]{10,11}$", ErrorMessage = "Số điện thoại phải có 10 hoặc 11 chữ số")]
        [Display(Name = "Số điện thoại")]
        public string CustomerPhone { get; set; } = "";

        [Display(Name = "Địa chỉ")]
        public string? CustomerAddress { get; set; }

        [Display(Name = "Ghi chú đặc biệt")]
        public string? SpecialRequests { get; set; }

        // Phương thức thanh toán
        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        [Display(Name = "Phương thức thanh toán")]
        public string PaymentMethod { get; set; } = ""; // "cash" hoặc "momo"

        // Tính toán giá
        public int TotalNights => (CheckOutDate - CheckInDate).Days;
        public decimal TotalAmount => RoomPrice * TotalNights;
        public decimal DepositAmount => PaymentMethod == "cash" ? TotalAmount * 0.3m : TotalAmount;
        public decimal RemainingAmount => TotalAmount - DepositAmount;

        // Trạng thái
        public bool IsValidDates => CheckInDate < CheckOutDate && CheckInDate >= DateTime.Today;
    }

    public class PaymentResultViewModel
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = "";
        public string BookingCode { get; set; } = "";
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "";
        public DateTime BookingDate { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string RoomName { get; set; } = "";
    }
}
