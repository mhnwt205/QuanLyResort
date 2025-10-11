using System.ComponentModel.DataAnnotations;

namespace QuanLyResort.ViewModels
{
    public class InventoryViewModel
    {
        public int InventoryId { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int QuantityOnHand { get; set; }
        public int QuantityReserved { get; set; }
        public int AvailableQuantity { get; set; }
        public int MinStockLevel { get; set; }
        public bool IsLowStock { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class StockIssueViewModel
    {
        public int IssueId { get; set; }
        public string IssueNumber { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int? RequestedBy { get; set; }
        public string RequestedByName { get; set; } = string.Empty;
        public int? IssuedBy { get; set; }
        public string IssuedByName { get; set; } = string.Empty;
        public int? ApprovedBy { get; set; }
        public string ApprovedByName { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<StockIssueItemViewModel> Items { get; set; } = new();
    }

    public class StockIssueItemViewModel
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public int QuantityRequested { get; set; }
        public int QuantityIssued { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
    }

    public class CreateStockIssueDto
    {
        [Required(ErrorMessage = "Vui lòng chọn kho")]
        public int WarehouseId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phòng ban")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mục đích")]
        public string Purpose { get; set; } = string.Empty;

        public List<StockIssueItemDto> Items { get; set; } = new();
    }

    public class StockIssueItemDto
    {
        [Required(ErrorMessage = "Vui lòng chọn vật tư")]
        public int ItemId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }
    }

    public class ItemViewModel
    {
        public int ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public string Unit { get; set; } = string.Empty;
        public int MinStockLevel { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<InventoryViewModel> Inventories { get; set; } = new();
    }

    public class WarehouseViewModel
    {
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int? ManagerId { get; set; }
        public string ManagerName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalValue { get; set; }
    }
}
