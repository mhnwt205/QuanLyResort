using System;
using System.Collections.Generic;

namespace QuanLyResort.Models;

public partial class ServiceBooking
{
    public int ServiceBookingId { get; set; }

    public string BookingCode { get; set; } = null!;

    public int? CustomerId { get; set; }

    public int? ServiceId { get; set; }

    public DateOnly BookingDate { get; set; }

    public DateOnly ServiceDate { get; set; }

    public int? Quantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public decimal? TotalAmount { get; set; }

    public string? Status { get; set; }

    public string? SpecialRequests { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Service? Service { get; set; }
}
