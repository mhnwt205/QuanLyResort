using System.ComponentModel.DataAnnotations;

namespace QuanLyResort.Models
{
    public class OnlinePayment
    {
        [Key]
        public int OnlinePaymentId { get; set; }
        
        public int BookingId { get; set; }
        
        public decimal Amount { get; set; }
        
        public string PaymentMethod { get; set; } = ""; // "cash" hoặc "momo"
        
        public string Status { get; set; } = "pending"; // "pending", "completed", "failed"
        
        public string? TransactionId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        public DateTime? CompletedAt { get; set; }
        
        public virtual Booking Booking { get; set; } = null!;
    }
}
