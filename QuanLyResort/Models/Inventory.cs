using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyResort.Models;

public partial class Inventory
{
    [Key]
    public int InventoryId { get; set; }

    public int? ItemId { get; set; }

    public int? WarehouseId { get; set; }

    [Required]
    public int QuantityOnHand { get; set; } = 0;

    [Required]
    public int QuantityReserved { get; set; } = 0;

    [Required]
    public int MinStockLevel { get; set; } = 10;

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitCost { get; set; } = 0;

    [Required]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public virtual Item? Item { get; set; }

    public virtual Warehouse? Warehouse { get; set; }
}
