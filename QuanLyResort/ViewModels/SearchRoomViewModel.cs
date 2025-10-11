using System.ComponentModel.DataAnnotations;

namespace QuanLyResort.ViewModels
{
    public class SearchRoomViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn ngày nhận phòng")]
        [Display(Name = "Ngày Nhận Phòng")]
        public DateTime CheckInDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Vui lòng chọn ngày trả phòng")]
        [Display(Name = "Ngày Trả Phòng")]
        public DateTime CheckOutDate { get; set; } = DateTime.Today.AddDays(1);

        [Required(ErrorMessage = "Vui lòng chọn số khách")]
        [Range(1, 10, ErrorMessage = "Số khách phải từ 1 đến 10")]
        [Display(Name = "Số Khách")]
        public int GuestCount { get; set; } = 1;

        [Display(Name = "Loại Phòng")]
        public int? RoomTypeId { get; set; }

        [Display(Name = "Giá Tối Đa")]
        public decimal? MaxPrice { get; set; }

        [Display(Name = "Từ Khóa Tìm Kiếm")]
        public string? SearchKeyword { get; set; }
    }

    public class RoomSearchResultViewModel
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string RoomTypeName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int MaxOccupancy { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Amenities { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
