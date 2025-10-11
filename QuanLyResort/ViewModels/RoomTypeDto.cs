namespace QuanLyResort.ViewModels
{
    public class RoomTypeDto
    {
        public int RoomTypeId { get; set; }
        public string TypeName { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }
        public decimal BaseRate { get; set; }
        public int MaxOccupancy { get; set; }
        public int Capacity { get; set; }
        public string? Amenities { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateRoomTypeDto
    {
        public string TypeName { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal? BasePrice { get; set; }
        public decimal? BaseRate { get; set; }
        public int? MaxOccupancy { get; set; }
        public int? Capacity { get; set; }
        public string? Amenities { get; set; }
    }
}
