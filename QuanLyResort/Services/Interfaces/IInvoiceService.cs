using QuanLyResort.ViewModels;

namespace QuanLyResort.Services.Interfaces
{
    public interface IInvoiceService
    {
        Task<IEnumerable<InvoiceDto>> GetAllAsync();
        Task<InvoiceDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateInvoiceDto dto, string createdByUser);
        Task<int> CreateFromBookingAsync(int bookingId, decimal taxRate = 0.1m, decimal discount = 0, string createdByUser = "system");
        Task<bool> UpdateAsync(int id, CreateInvoiceDto dto, string updatedByUser);
        Task<bool> ApproveAsync(int id, string approvedByUser);
        Task<bool> CancelAsync(int id, string cancelledByUser);
        Task<decimal> GetRemainingAmountAsync(int invoiceId);
        Task<bool> IsFullyPaidAsync(int invoiceId);
        Task<IEnumerable<InvoiceDto>> GetByCustomerIdAsync(int customerId);
        Task<IEnumerable<InvoiceDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<InvoiceDto>> GetByStatusAsync(string status);
        Task<string> GenerateInvoiceNumberAsync();
    }
}
