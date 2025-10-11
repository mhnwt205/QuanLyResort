using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyResort.Models;

public partial class StockIssue
{
    [Key]
    public int IssueId { get; set; }

    [Required]
    [StringLength(50)]
    public string IssueNumber { get; set; } = null!;

    public int? WarehouseId { get; set; }

    public int? DepartmentId { get; set; }

    [StringLength(500)]
    public string? Purpose { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "pending";

    public int? RequestedBy { get; set; }

    public int? ApprovedBy { get; set; }

    public int? IssuedBy { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public virtual Warehouse? Warehouse { get; set; }

    public virtual Department? Department { get; set; }

    public virtual Employee? RequestedByNavigation { get; set; }

    public virtual Employee? ApprovedByNavigation { get; set; }

    public virtual Employee? IssuedByNavigation { get; set; }

    public virtual ICollection<StockIssueItem> StockIssueItems { get; set; } = new List<StockIssueItem>();
}