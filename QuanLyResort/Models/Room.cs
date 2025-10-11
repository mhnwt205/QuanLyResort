using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyResort.Models;

public partial class Room
{
    [Key]
    public int RoomId { get; set; }

    [Required]
    [StringLength(10)]
    public string RoomNumber { get; set; } = null!;

    [Required]
    public int RoomTypeId { get; set; }

    [Required]
    public int FloorNumber { get; set; }

    [Required]
    public string Status { get; set; } = "available";

    // Not present in DB schema (Rooms table). Avoid mapping to prevent SQL errors.
    [NotMapped]
    public decimal Price { get; set; } = 0;

    [Required]
    // Not present in DB schema (Rooms table). Use RoomType.MaxOccupancy instead.
    [NotMapped]
    public int MaxOccupancy { get; set; } = 2;

    // Not present in DB schema (Rooms table). Use RoomType.Description instead.
    [NotMapped]
    public string? Description { get; set; }

    public DateTime? LastCleaned { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();

    public virtual RoomType? RoomType { get; set; }
}
