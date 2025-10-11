namespace QuanLyResort.ViewModels
{
    public class ServiceDto
    {
        public int ServiceId { get; set; }
        public string ServiceCode { get; set; } = null!;
        public string ServiceName { get; set; } = null!;
        public int? CategoryId { get; set; }
        public int ServiceCategory { get; set; }
        public string ServiceCategoryText { get; set; } = null!;
        public string? Description { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Unit { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateServiceDto
    {
        public string ServiceCode { get; set; } = null!;
        public string ServiceName { get; set; } = null!;
        public int? CategoryId { get; set; }
        public int? ServiceCategory { get; set; }
        public string? Description { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? Unit { get; set; }
        public bool? IsActive { get; set; }
    }
}
