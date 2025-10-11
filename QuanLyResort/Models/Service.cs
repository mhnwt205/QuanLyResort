using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyResort.Models;

public partial class Service
{
    [Key]
    public int ServiceId { get; set; }

    [Required]
    [StringLength(20)]
    public string ServiceCode { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string ServiceName { get; set; } = null!;

    public int? CategoryId { get; set; }

    [Required]
    public string ServiceCategory { get; set; } = "Other";

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; } = 0;

    [StringLength(20)]
    public string? Unit { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public virtual ServiceCategory? Category { get; set; }

    public virtual ICollection<ServiceBooking> ServiceBookings { get; set; } = new List<ServiceBooking>();
}
