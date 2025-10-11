using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyResort.Models;

public partial class StockIssueItem
{
    [Key]
    public int StockIssueItemId { get; set; }

    [Required]
    public int IssueId { get; set; }

    [Required]
    public int ItemId { get; set; }

    [Required]
    public int QuantityRequested { get; set; }

    [Required]
    public int QuantityIssued { get; set; } = 0;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitCost { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCost { get; set; } = 0;

    [StringLength(200)]
    public string? Notes { get; set; }

    public virtual StockIssue? StockIssue { get; set; }

    public virtual Item? Item { get; set; }
}
