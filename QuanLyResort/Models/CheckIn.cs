using System;
using System.Collections.Generic;

namespace QuanLyResort.Models;

public partial class CheckIn
{
    public int CheckInId { get; set; }

    public int? BookingId { get; set; }

    public int? RoomId { get; set; }

    public DateTime? CheckInTime { get; set; }

    public DateTime? CheckOutTime { get; set; }

    public int? ActualAdults { get; set; }

    public int? ActualChildren { get; set; }

    public int? CheckedInBy { get; set; }

    public int? CheckedOutBy { get; set; }

    public string? Notes { get; set; }

    public virtual Booking? Booking { get; set; }

    public virtual User? CheckedInByNavigation { get; set; }

    public virtual User? CheckedOutByNavigation { get; set; }

    public virtual Room? Room { get; set; }
}
