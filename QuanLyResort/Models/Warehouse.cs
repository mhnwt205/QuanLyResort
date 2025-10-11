using System;
using System.Collections.Generic;

namespace QuanLyResort.Models;

public partial class Warehouse
{
    public int WarehouseId { get; set; }

    public string WarehouseName { get; set; } = null!;

    public string? Location { get; set; }

    public int? ManagerId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    public virtual Employee? Manager { get; set; }

    public virtual ICollection<StockIssue> StockIssues { get; set; } = new List<StockIssue>();
}
