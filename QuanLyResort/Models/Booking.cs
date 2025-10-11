using System;
using System.Collections.Generic;

namespace QuanLyResort.Models;

public partial class Booking
{
    public int BookingId { get; set; }

    public string BookingCode { get; set; } = null!;

    public int? CustomerId { get; set; }

    public int? RoomId { get; set; }

    public DateOnly CheckInDate { get; set; }

    public DateOnly CheckOutDate { get; set; }

    public int? Adults { get; set; }

    public int? Children { get; set; }

    public string? Status { get; set; }

    public decimal? TotalAmount { get; set; }

    public decimal? DepositAmount { get; set; }

    public string? SpecialRequests { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<OnlinePayment> OnlinePayments { get; set; } = new List<OnlinePayment>();

    public virtual Room? Room { get; set; }
}
