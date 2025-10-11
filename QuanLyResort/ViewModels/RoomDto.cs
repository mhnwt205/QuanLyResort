namespace QuanLyResort.ViewModels
{
    public class RoomDto
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = null!;
        public int RoomTypeId { get; set; }
        public string RoomTypeName { get; set; } = null!;
        public int FloorNumber { get; set; }
        public int Status { get; set; }
        public string StatusText { get; set; } = null!;
        public decimal Price { get; set; }
        public int MaxOccupancy { get; set; }
        public string? Description { get; set; }
        public DateTime? LastCleaned { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateRoomDto
    {
        public string RoomNumber { get; set; } = null!;
        public int RoomTypeId { get; set; }
        public int FloorNumber { get; set; }
        public int? Status { get; set; }
        public decimal? Price { get; set; }
        public int? MaxOccupancy { get; set; }
        public string? Description { get; set; }
        public string? Notes { get; set; }
    }
}
