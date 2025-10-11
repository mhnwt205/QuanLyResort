using QuanLyResort.ViewModels;

namespace QuanLyResort.Services.Interfaces
{
    public interface IBookingService
    {
        Task<IEnumerable<BookingViewModel>> GetAllAsync();
        Task<BookingViewModel?> GetByIdAsync(int id);
        Task<BookingDetailsViewModel?> GetDetailsByIdAsync(int id);
        Task<int> CreateAsync(CreateBookingDto dto, string createdByUser);
        Task<bool> ConfirmBookingAsync(int bookingId, int assignedRoomId, string performedBy);
        Task<bool> AssignRoomAsync(int bookingId, int roomId, string performedBy);
        Task<bool> CheckInAsync(int bookingId, string performedBy);
        Task<bool> CheckOutAsync(int bookingId, string performedBy);
        Task<bool> CancelBookingAsync(int bookingId, string performedBy);
        Task<IEnumerable<BookingViewModel>> GetByCustomerIdAsync(int customerId);
        Task<IEnumerable<BookingViewModel>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<BookingViewModel>> GetByStatusAsync(string status);
        Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeBookingId = null);
        Task<decimal> CalculateTotalAmountAsync(int roomId, DateTime checkIn, DateTime checkOut);
    }
}
