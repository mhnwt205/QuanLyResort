using QuanLyResort.ViewModels;

namespace QuanLyResort.Services.Interfaces
{
    public interface IServiceBookingService
    {
        Task<IEnumerable<ServiceBookingDto>> GetAllAsync();
        Task<ServiceBookingDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateServiceBookingDto dto, string createdByUser);
        Task<bool> UpdateAsync(int id, CreateServiceBookingDto dto, string updatedByUser);
        Task<bool> ApproveAsync(int id, string approvedByUser);
        Task<bool> CompleteAsync(int id, string completedByUser);
        Task<bool> CancelAsync(int id, string cancelledByUser);
        Task<IEnumerable<ServiceBookingDto>> GetByCustomerIdAsync(int customerId);
        Task<IEnumerable<ServiceBookingDto>> GetByServiceIdAsync(int serviceId);
        Task<IEnumerable<ServiceBookingDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<ServiceBookingDto>> GetByStatusAsync(string status);
        Task<decimal> CalculateTotalAmountAsync(int serviceId, int quantity);
        Task<string> GenerateBookingCodeAsync();
    }
}
