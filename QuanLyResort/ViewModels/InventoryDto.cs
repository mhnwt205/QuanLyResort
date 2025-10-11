namespace QuanLyResort.ViewModels
{
    public class InventoryDto
    {
        public int InventoryId { get; set; }
        public int? ItemId { get; set; }
        public int? WarehouseId { get; set; }
        public int QuantityOnHand { get; set; }
        public int QuantityReserved { get; set; }
        public int MinStockLevel { get; set; }
        public decimal UnitCost { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class CreateInventoryDto
    {
        public int? ItemId { get; set; }
        public int? WarehouseId { get; set; }
        public int? QuantityOnHand { get; set; }
        public int? QuantityReserved { get; set; }
        public int? MinStockLevel { get; set; }
        public decimal? UnitCost { get; set; }
    }
}
