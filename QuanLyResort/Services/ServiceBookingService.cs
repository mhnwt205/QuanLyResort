using Microsoft.EntityFrameworkCore;
using QuanLyResort.Models;
using QuanLyResort.Services.Interfaces;
using QuanLyResort.ViewModels;

namespace QuanLyResort.Services
{
    public class ServiceBookingService : IServiceBookingService
    {
        private readonly ResortDbContext _context;
        private readonly ILogger<ServiceBookingService> _logger;

        public ServiceBookingService(ResortDbContext context, ILogger<ServiceBookingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<ServiceBookingDto>> GetAllAsync()
        {
            try
            {
                var serviceBookings = await _context.ServiceBookings
                    .Include(sb => sb.Customer)
                    .Include(sb => sb.Service)
                    .Include(sb => sb.CreatedByNavigation)
                    .OrderByDescending(sb => sb.CreatedAt)
                    .Select(sb => new ServiceBookingDto
                    {
                        ServiceBookingId = sb.ServiceBookingId,
                        BookingCode = sb.BookingCode,
                        CustomerId = sb.CustomerId,
                        CustomerName = sb.Customer != null ? $"{sb.Customer.FirstName} {sb.Customer.LastName}" : "",
                        ServiceId = sb.ServiceId ?? 0,
                        ServiceName = sb.Service != null ? sb.Service.ServiceName : "",
                        BookingDate = sb.BookingDate.ToDateTime(TimeOnly.MinValue),
                        ServiceDate = sb.ServiceDate.ToDateTime(TimeOnly.MinValue),
                        Quantity = sb.Quantity ?? 1,
                        UnitPrice = sb.UnitPrice ?? 0,
                        TotalAmount = sb.TotalAmount ?? 0,
                        Status = sb.Status ?? "",
                        SpecialRequests = sb.SpecialRequests ?? "",
                        CreatedAt = sb.CreatedAt ?? DateTime.Now,
                        CreatedBy = sb.CreatedByNavigation != null ? sb.CreatedByNavigation.Username : ""
                    })
                    .ToListAsync();

                return serviceBookings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all service bookings");
                throw;
            }
        }

        public async Task<ServiceBookingDto?> GetByIdAsync(int id)
        {
            try
            {
                var serviceBooking = await _context.ServiceBookings
                    .Include(sb => sb.Customer)
                    .Include(sb => sb.Service)
                    .Include(sb => sb.CreatedByNavigation)
                    .FirstOrDefaultAsync(sb => sb.ServiceBookingId == id);

                if (serviceBooking == null) return null;

                return new ServiceBookingDto
                {
                    ServiceBookingId = serviceBooking.ServiceBookingId,
                    BookingCode = serviceBooking.BookingCode,
                    CustomerId = serviceBooking.CustomerId,
                    CustomerName = serviceBooking.Customer != null ? $"{serviceBooking.Customer.FirstName} {serviceBooking.Customer.LastName}" : "",
                    ServiceId = serviceBooking.ServiceId ?? 0,
                    ServiceName = serviceBooking.Service != null ? serviceBooking.Service.ServiceName : "",
                    BookingDate = serviceBooking.BookingDate.ToDateTime(TimeOnly.MinValue),
                    ServiceDate = serviceBooking.ServiceDate.ToDateTime(TimeOnly.MinValue),
                    Quantity = serviceBooking.Quantity ?? 1,
                    UnitPrice = serviceBooking.UnitPrice ?? 0,
                    TotalAmount = serviceBooking.TotalAmount ?? 0,
                    Status = serviceBooking.Status ?? "",
                    SpecialRequests = serviceBooking.SpecialRequests ?? "",
                    CreatedAt = serviceBooking.CreatedAt ?? DateTime.Now,
                    CreatedBy = serviceBooking.CreatedByNavigation != null ? serviceBooking.CreatedByNavigation.Username : ""
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service booking by id {ServiceBookingId}", id);
                throw;
            }
        }

        public async Task<int> CreateAsync(CreateServiceBookingDto dto, string createdByUser)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var service = await _context.Services.FindAsync(dto.ServiceId);
                if (service == null)
                {
                    throw new InvalidOperationException("Service not found");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == createdByUser);
                var bookingCode = await GenerateBookingCodeAsync();

                var totalAmount = await CalculateTotalAmountAsync(dto.ServiceId, dto.Quantity);

                var serviceBooking = new ServiceBooking
                {
                    BookingCode = bookingCode,
                    CustomerId = dto.CustomerId,
                    ServiceId = dto.ServiceId,
                    BookingDate = DateOnly.FromDateTime(dto.BookingDate),
                    ServiceDate = DateOnly.FromDateTime(dto.ServiceDate),
                    Quantity = dto.Quantity,
                    UnitPrice = service.UnitPrice,
                    TotalAmount = totalAmount,
                    Status = "pending",
                    SpecialRequests = dto.SpecialRequests,
                    CreatedBy = user?.UserId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.ServiceBookings.Add(serviceBooking);

                // Log audit
                await LogAuditAsync(user?.UserId, "CREATE", "ServiceBookings", serviceBooking.ServiceBookingId, null, serviceBooking);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Service booking created successfully. ServiceBookingId: {ServiceBookingId}, Code: {BookingCode}", 
                    serviceBooking.ServiceBookingId, serviceBooking.BookingCode);

                return serviceBooking.ServiceBookingId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating service booking");
                throw;
            }
        }

        public async Task<bool> UpdateAsync(int id, CreateServiceBookingDto dto, string updatedByUser)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var serviceBooking = await _context.ServiceBookings.FindAsync(id);
                if (serviceBooking == null) return false;

                var service = await _context.Services.FindAsync(dto.ServiceId);
                if (service == null)
                {
                    throw new InvalidOperationException("Service not found");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == updatedByUser);
                var oldValues = new { serviceBooking.ServiceId, serviceBooking.Quantity, serviceBooking.TotalAmount };

                serviceBooking.CustomerId = dto.CustomerId;
                serviceBooking.ServiceId = dto.ServiceId;
                serviceBooking.BookingDate = DateOnly.FromDateTime(dto.BookingDate);
                serviceBooking.ServiceDate = DateOnly.FromDateTime(dto.ServiceDate);
                serviceBooking.Quantity = dto.Quantity;
                serviceBooking.UnitPrice = service.UnitPrice;
                serviceBooking.TotalAmount = await CalculateTotalAmountAsync(dto.ServiceId, dto.Quantity);
                serviceBooking.SpecialRequests = dto.SpecialRequests;
                serviceBooking.UpdatedAt = DateTime.Now;

                _context.ServiceBookings.Update(serviceBooking);

                // Log audit
                await LogAuditAsync(user?.UserId, "UPDATE", "ServiceBookings", id, oldValues, serviceBooking);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Service booking updated successfully. ServiceBookingId: {ServiceBookingId}", id);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating service booking {ServiceBookingId}", id);
                return false;
            }
        }

        public async Task<bool> ApproveAsync(int id, string approvedByUser)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var serviceBooking = await _context.ServiceBookings.FindAsync(id);
                if (serviceBooking == null) return false;

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == approvedByUser);
                var oldValues = new { serviceBooking.Status };

                serviceBooking.Status = "confirmed";
                serviceBooking.UpdatedAt = DateTime.Now;

                _context.ServiceBookings.Update(serviceBooking);

                // Log audit
                await LogAuditAsync(user?.UserId, "APPROVE", "ServiceBookings", id, oldValues, serviceBooking);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Service booking approved successfully. ServiceBookingId: {ServiceBookingId}", id);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error approving service booking {ServiceBookingId}", id);
                return false;
            }
        }

        public async Task<bool> CompleteAsync(int id, string completedByUser)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var serviceBooking = await _context.ServiceBookings.FindAsync(id);
                if (serviceBooking == null) return false;

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == completedByUser);
                var oldValues = new { serviceBooking.Status };

                serviceBooking.Status = "completed";
                serviceBooking.UpdatedAt = DateTime.Now;

                _context.ServiceBookings.Update(serviceBooking);

                // Log audit
                await LogAuditAsync(user?.UserId, "COMPLETE", "ServiceBookings", id, oldValues, serviceBooking);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Service booking completed successfully. ServiceBookingId: {ServiceBookingId}", id);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error completing service booking {ServiceBookingId}", id);
                return false;
            }
        }

        public async Task<bool> CancelAsync(int id, string cancelledByUser)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var serviceBooking = await _context.ServiceBookings.FindAsync(id);
                if (serviceBooking == null) return false;

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == cancelledByUser);
                var oldValues = new { serviceBooking.Status };

                serviceBooking.Status = "cancelled";
                serviceBooking.UpdatedAt = DateTime.Now;

                _context.ServiceBookings.Update(serviceBooking);

                // Log audit
                await LogAuditAsync(user?.UserId, "CANCEL", "ServiceBookings", id, oldValues, serviceBooking);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Service booking cancelled successfully. ServiceBookingId: {ServiceBookingId}", id);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error cancelling service booking {ServiceBookingId}", id);
                return false;
            }
        }

        public async Task<IEnumerable<ServiceBookingDto>> GetByCustomerIdAsync(int customerId)
        {
            try
            {
                var serviceBookings = await _context.ServiceBookings
                    .Include(sb => sb.Customer)
                    .Include(sb => sb.Service)
                    .Where(sb => sb.CustomerId == customerId)
                    .OrderByDescending(sb => sb.CreatedAt)
                    .Select(sb => new ServiceBookingDto
                    {
                        ServiceBookingId = sb.ServiceBookingId,
                        BookingCode = sb.BookingCode,
                        CustomerId = sb.CustomerId,
                        CustomerName = sb.Customer != null ? $"{sb.Customer.FirstName} {sb.Customer.LastName}" : "",
                        ServiceId = sb.ServiceId ?? 0,
                        ServiceName = sb.Service != null ? sb.Service.ServiceName : "",
                        BookingDate = sb.BookingDate.ToDateTime(TimeOnly.MinValue),
                        ServiceDate = sb.ServiceDate.ToDateTime(TimeOnly.MinValue),
                        Quantity = sb.Quantity ?? 1,
                        UnitPrice = sb.UnitPrice ?? 0,
                        TotalAmount = sb.TotalAmount ?? 0,
                        Status = sb.Status ?? "",
                        SpecialRequests = sb.SpecialRequests ?? "",
                        CreatedAt = sb.CreatedAt ?? DateTime.Now,
                        CreatedBy = sb.CreatedByNavigation != null ? sb.CreatedByNavigation.Username : ""
                    })
                    .ToListAsync();

                return serviceBookings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service bookings by customer id {CustomerId}", customerId);
                throw;
            }
        }

        public async Task<IEnumerable<ServiceBookingDto>> GetByServiceIdAsync(int serviceId)
        {
            try
            {
                var serviceBookings = await _context.ServiceBookings
                    .Include(sb => sb.Customer)
                    .Include(sb => sb.Service)
                    .Where(sb => sb.ServiceId == serviceId)
                    .OrderByDescending(sb => sb.CreatedAt)
                    .Select(sb => new ServiceBookingDto
                    {
                        ServiceBookingId = sb.ServiceBookingId,
                        BookingCode = sb.BookingCode,
                        CustomerId = sb.CustomerId,
                        CustomerName = sb.Customer != null ? $"{sb.Customer.FirstName} {sb.Customer.LastName}" : "",
                        ServiceId = sb.ServiceId ?? 0,
                        ServiceName = sb.Service != null ? sb.Service.ServiceName : "",
                        BookingDate = sb.BookingDate.ToDateTime(TimeOnly.MinValue),
                        ServiceDate = sb.ServiceDate.ToDateTime(TimeOnly.MinValue),
                        Quantity = sb.Quantity ?? 1,
                        UnitPrice = sb.UnitPrice ?? 0,
                        TotalAmount = sb.TotalAmount ?? 0,
                        Status = sb.Status ?? "",
                        SpecialRequests = sb.SpecialRequests ?? "",
                        CreatedAt = sb.CreatedAt ?? DateTime.Now,
                        CreatedBy = sb.CreatedByNavigation != null ? sb.CreatedByNavigation.Username : ""
                    })
                    .ToListAsync();

                return serviceBookings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service bookings by service id {ServiceId}", serviceId);
                throw;
            }
        }

        public async Task<IEnumerable<ServiceBookingDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var start = DateOnly.FromDateTime(startDate);
                var end = DateOnly.FromDateTime(endDate);

                var serviceBookings = await _context.ServiceBookings
                    .Include(sb => sb.Customer)
                    .Include(sb => sb.Service)
                    .Where(sb => sb.ServiceDate >= start && sb.ServiceDate <= end)
                    .OrderByDescending(sb => sb.CreatedAt)
                    .Select(sb => new ServiceBookingDto
                    {
                        ServiceBookingId = sb.ServiceBookingId,
                        BookingCode = sb.BookingCode,
                        CustomerId = sb.CustomerId,
                        CustomerName = sb.Customer != null ? $"{sb.Customer.FirstName} {sb.Customer.LastName}" : "",
                        ServiceId = sb.ServiceId ?? 0,
                        ServiceName = sb.Service != null ? sb.Service.ServiceName : "",
                        BookingDate = sb.BookingDate.ToDateTime(TimeOnly.MinValue),
                        ServiceDate = sb.ServiceDate.ToDateTime(TimeOnly.MinValue),
                        Quantity = sb.Quantity ?? 1,
                        UnitPrice = sb.UnitPrice ?? 0,
                        TotalAmount = sb.TotalAmount ?? 0,
                        Status = sb.Status ?? "",
                        SpecialRequests = sb.SpecialRequests ?? "",
                        CreatedAt = sb.CreatedAt ?? DateTime.Now,
                        CreatedBy = sb.CreatedByNavigation != null ? sb.CreatedByNavigation.Username : ""
                    })
                    .ToListAsync();

                return serviceBookings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service bookings by date range {StartDate} - {EndDate}", startDate, endDate);
                throw;
            }
        }

        public async Task<IEnumerable<ServiceBookingDto>> GetByStatusAsync(string status)
        {
            try
            {
                var serviceBookings = await _context.ServiceBookings
                    .Include(sb => sb.Customer)
                    .Include(sb => sb.Service)
                    .Where(sb => sb.Status == status)
                    .OrderByDescending(sb => sb.CreatedAt)
                    .Select(sb => new ServiceBookingDto
                    {
                        ServiceBookingId = sb.ServiceBookingId,
                        BookingCode = sb.BookingCode,
                        CustomerId = sb.CustomerId,
                        CustomerName = sb.Customer != null ? $"{sb.Customer.FirstName} {sb.Customer.LastName}" : "",
                        ServiceId = sb.ServiceId ?? 0,
                        ServiceName = sb.Service != null ? sb.Service.ServiceName : "",
                        BookingDate = sb.BookingDate.ToDateTime(TimeOnly.MinValue),
                        ServiceDate = sb.ServiceDate.ToDateTime(TimeOnly.MinValue),
                        Quantity = sb.Quantity ?? 1,
                        UnitPrice = sb.UnitPrice ?? 0,
                        TotalAmount = sb.TotalAmount ?? 0,
                        Status = sb.Status ?? "",
                        SpecialRequests = sb.SpecialRequests ?? "",
                        CreatedAt = sb.CreatedAt ?? DateTime.Now,
                        CreatedBy = sb.CreatedByNavigation != null ? sb.CreatedByNavigation.Username : ""
                    })
                    .ToListAsync();

                return serviceBookings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service bookings by status {Status}", status);
                throw;
            }
        }

        public async Task<decimal> CalculateTotalAmountAsync(int serviceId, int quantity)
        {
            try
            {
                var service = await _context.Services.FindAsync(serviceId);
                if (service == null) return 0;

                return service.UnitPrice * quantity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total amount for service {ServiceId}", serviceId);
                throw;
            }
        }

        public async Task<string> GenerateBookingCodeAsync()
        {
            try
            {
                var today = DateTime.Today;
                var prefix = $"SRV{today:yyyyMMdd}";
                var lastBooking = await _context.ServiceBookings
                    .Where(sb => sb.BookingCode.StartsWith(prefix))
                    .OrderByDescending(sb => sb.BookingCode)
                    .FirstOrDefaultAsync();

                if (lastBooking == null)
                {
                    return $"{prefix}001";
                }

                var lastNumber = int.Parse(lastBooking.BookingCode.Substring(prefix.Length));
                return $"{prefix}{(lastNumber + 1):D3}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating service booking code");
                throw;
            }
        }

        private async Task LogAuditAsync(int? userId, string action, string tableName, int recordId, object? oldValues, object? newValues)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    TableName = tableName,
                    RecordId = recordId,
                    OldValues = oldValues != null ? System.Text.Json.JsonSerializer.Serialize(oldValues) : null,
                    NewValues = newValues != null ? System.Text.Json.JsonSerializer.Serialize(newValues) : null,
                    IpAddress = "127.0.0.1",
                    UserAgent = "System",
                    CreatedAt = DateTime.Now
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging audit for {Action} on {TableName} {RecordId}", action, tableName, recordId);
            }
        }
    }
}
