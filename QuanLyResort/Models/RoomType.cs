using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyResort.Models;

public partial class RoomType
{
    [Key]
    public int RoomTypeId { get; set; }

    [Required]
    [StringLength(100)]
    public string TypeName { get; set; } = null!;

    // Column 'Name' does not exist in DB; keep property for UI but ignore mapping
    [NotMapped]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal BasePrice { get; set; } = 0;

    // Column 'BaseRate' does not exist in DB
    [NotMapped]
    public decimal BaseRate { get; set; } = 0;

    [Required]
    public int MaxOccupancy { get; set; } = 2;

    // Column 'Capacity' does not exist in DB
    [NotMapped]
    public int Capacity { get; set; } = 2;

    [StringLength(1000)]
    public string? Amenities { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
