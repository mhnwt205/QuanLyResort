using System;
using System.Collections.Generic;

namespace QuanLyResort.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public string PaymentNumber { get; set; } = null!;

    public int? InvoiceId { get; set; }

    public DateOnly PaymentDate { get; set; }

    public decimal Amount { get; set; }

    public string? PaymentMethod { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? Notes { get; set; }

    public int? ProcessedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Invoice? Invoice { get; set; }

    public virtual User? ProcessedByNavigation { get; set; }
}
