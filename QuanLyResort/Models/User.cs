using System;
using System.Collections.Generic;

namespace QuanLyResort.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public int? EmployeeId { get; set; }

    public int? RoleId { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? LastLogin { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<CheckIn> CheckInCheckedInByNavigations { get; set; } = new List<CheckIn>();

    public virtual ICollection<CheckIn> CheckInCheckedOutByNavigations { get; set; } = new List<CheckIn>();

    public virtual Employee? Employee { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Role? Role { get; set; }

    public virtual ICollection<ServiceBooking> ServiceBookings { get; set; } = new List<ServiceBooking>();

    public virtual ICollection<StockIssue> StockIssueApprovedByNavigations { get; set; } = new List<StockIssue>();

    public virtual ICollection<StockIssue> StockIssueIssuedByNavigations { get; set; } = new List<StockIssue>();

    public virtual ICollection<StockIssue> StockIssueRequestedByNavigations { get; set; } = new List<StockIssue>();
}
