using System;
using System.Collections.Generic;

namespace QuanLyResort.Models;

public partial class Item
{
    public int ItemId { get; set; }

    public string ItemCode { get; set; } = null!;

    public string ItemName { get; set; } = null!;

    public string? Description { get; set; }

    public string Unit { get; set; } = null!;

    public decimal? CostPrice { get; set; }

    public decimal? SellingPrice { get; set; }

    public int? MinStockLevel { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
}
