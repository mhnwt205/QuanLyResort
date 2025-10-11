using System;
using System.Collections.Generic;

namespace QuanLyResort.Models;

public partial class Invoice
{
    public int InvoiceId { get; set; }

    public string InvoiceNumber { get; set; } = null!;

    public int? CustomerId { get; set; }

    public int? BookingId { get; set; }

    public DateOnly InvoiceDate { get; set; }

    public DateOnly? DueDate { get; set; }

    public decimal Subtotal { get; set; }

    public decimal? TaxAmount { get; set; }

    public decimal? DiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Status { get; set; }

    public string? PaymentMethod { get; set; }

    public string? Notes { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Booking? Booking { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
