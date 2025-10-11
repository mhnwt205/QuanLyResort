using System;
using System.Collections.Generic;

namespace QuanLyResort.Models;

public partial class InvoiceItem
{
    public int ItemId { get; set; }

    public int? InvoiceId { get; set; }

    public string? ItemType { get; set; }

    public string ItemName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal? Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Invoice? Invoice { get; set; }
}
