using QuanLyResort.ViewModels;

namespace QuanLyResort.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<IEnumerable<PaymentViewModel>> GetAllAsync();
        Task<PaymentViewModel?> GetByIdAsync(int id);
        Task<int> ProcessPaymentAsync(CreatePaymentDto dto, string processedByUser);
        Task<bool> RefundPaymentAsync(int paymentId, decimal amount, string reason, string processedByUser);
        Task<IEnumerable<PaymentViewModel>> GetByInvoiceIdAsync(int invoiceId);
        Task<IEnumerable<PaymentViewModel>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetTotalPaidAmountAsync(int invoiceId);
        Task<string> GeneratePaymentNumberAsync();
        Task<bool> ValidatePaymentAsync(CreatePaymentDto dto);
    }
}
