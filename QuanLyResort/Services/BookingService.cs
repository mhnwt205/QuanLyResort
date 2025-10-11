using Microsoft.EntityFrameworkCore;
using QuanLyResort.Models;
using QuanLyResort.Services.Interfaces;
using QuanLyResort.ViewModels;

namespace QuanLyResort.Services
{
    public class BookingService : IBookingService
    {
        private readonly ResortDbContext _context;
        private readonly ILogger<BookingService> _logger;

        public BookingService(ResortDbContext context, ILogger<BookingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<BookingViewModel>> GetAllAsync()
        {
            try
            {
                var bookings = await _context.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                    .Include(b => b.CreatedByNavigation)
                    .OrderByDescending(b => b.CreatedAt)
                    .Select(b => new BookingViewModel
                    {
                        BookingId = b.BookingId,
                        BookingCode = b.BookingCode,
                        CustomerId = b.CustomerId ?? 0,
                        CustomerName = b.Customer != null ? $"{b.Customer.FirstName} {b.Customer.LastName}" : "",
                        RoomId = b.RoomId,
                        RoomNumber = b.Room != null ? b.Room.RoomNumber : "",
                        RoomType = b.Room != null && b.Room.RoomType != null ? b.Room.RoomType.TypeName : "",
                        CheckInDate = b.CheckInDate.ToDateTime(TimeOnly.MinValue),
                        CheckOutDate = b.CheckOutDate.ToDateTime(TimeOnly.MinValue),
                        Adults = b.Adults ?? 1,
                        Children = b.Children ?? 0,
                        TotalAmount = b.TotalAmount ?? 0,
                        DepositAmount = b.DepositAmount,
                        Status = b.Status ?? "",
                        SpecialRequests = b.SpecialRequests ?? "",
                        CreatedAt = b.CreatedAt ?? DateTime.Now,
                        UpdatedAt = b.UpdatedAt,
                        CreatedBy = b.CreatedByNavigation != null ? b.CreatedByNavigation.Username : ""
                    })
                    .ToListAsync();

                return bookings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all bookings");
                throw;
            }
        }

        public async Task<BookingViewModel?> GetByIdAsync(int id)
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                    .Include(b => b.CreatedByNavigation)
                    .FirstOrDefaultAsync(b => b.BookingId == id);

                if (booking == null) return null;

                return new BookingViewModel
                {
                    BookingId = booking.BookingId,
                    BookingCode = booking.BookingCode,
                    CustomerId = booking.CustomerId ?? 0,
                    CustomerName = booking.Customer != null ? $"{booking.Customer.FirstName} {booking.Customer.LastName}" : "",
                    RoomId = booking.RoomId,
                    RoomNumber = booking.Room != null ? booking.Room.RoomNumber : "",
                    RoomType = booking.Room != null && booking.Room.RoomType != null ? booking.Room.RoomType.TypeName : "",
                    CheckInDate = booking.CheckInDate.ToDateTime(TimeOnly.MinValue),
                    CheckOutDate = booking.CheckOutDate.ToDateTime(TimeOnly.MinValue),
                    Adults = booking.Adults ?? 1,
                    Children = booking.Children ?? 0,
                    TotalAmount = booking.TotalAmount ?? 0,
                    DepositAmount = booking.DepositAmount,
                    Status = booking.Status ?? "",
                    SpecialRequests = booking.SpecialRequests ?? "",
                    CreatedAt = booking.CreatedAt ?? DateTime.Now,
                    UpdatedAt = booking.UpdatedAt,
                    CreatedBy = booking.CreatedByNavigation != null ? booking.CreatedByNavigation.Username : ""
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting booking by id {BookingId}", id);
                throw;
            }
        }

        public async Task<BookingDetailsViewModel?> GetDetailsByIdAsync(int id)
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                    .Include(b => b.CreatedByNavigation)
                    .Include(b => b.CheckIns)
                    .Include(b => b.Invoices)
                    .ThenInclude(i => i.InvoiceItems)
                    .FirstOrDefaultAsync(b => b.BookingId == id);

                if (booking == null) return null;

                var serviceBookings = await _context.ServiceBookings
                    .Include(sb => sb.Service)
                    .Where(sb => sb.CustomerId == booking.CustomerId &&
                               sb.ServiceDate >= booking.CheckInDate &&
                               sb.ServiceDate <= booking.CheckOutDate)
                    .Select(sb => new ServiceBookingDto
                    {
                        ServiceBookingId = sb.ServiceBookingId,
                        BookingCode = sb.BookingCode,
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

                var nights = (booking.CheckOutDate.ToDateTime(TimeOnly.MinValue) - booking.CheckInDate.ToDateTime(TimeOnly.MinValue)).Days;

                return new BookingDetailsViewModel
                {
                    BookingId = booking.BookingId,
                    BookingCode = booking.BookingCode,
                    CustomerId = booking.CustomerId ?? 0,
                    CustomerName = booking.Customer != null ? $"{booking.Customer.FirstName} {booking.Customer.LastName}" : "",
                    CustomerEmail = booking.Customer?.Email ?? "",
                    CustomerPhone = booking.Customer?.Phone ?? "",
                    RoomId = booking.RoomId,
                    RoomNumber = booking.Room != null ? booking.Room.RoomNumber : "",
                    RoomType = booking.Room != null && booking.Room.RoomType != null ? booking.Room.RoomType.TypeName : "",
                    RoomPrice = booking.Room != null ? booking.Room.Price : 0,
                    CheckInDate = booking.CheckInDate.ToDateTime(TimeOnly.MinValue),
                    CheckOutDate = booking.CheckOutDate.ToDateTime(TimeOnly.MinValue),
                    Adults = booking.Adults ?? 1,
                    Children = booking.Children ?? 0,
                    TotalAmount = booking.TotalAmount ?? 0,
                    DepositAmount = booking.DepositAmount,
                    Status = booking.Status ?? "",
                    SpecialRequests = booking.SpecialRequests ?? "",
                    CreatedAt = booking.CreatedAt ?? DateTime.Now,
                    UpdatedAt = booking.UpdatedAt,
                    CreatedBy = booking.CreatedByNavigation != null ? booking.CreatedByNavigation.Username : "",
                    Nights = nights,
                    ServiceBookings = serviceBookings.Select(sb => new ServiceBookingViewModel
                    {
                        ServiceBookingId = sb.ServiceBookingId,
                        BookingCode = sb.BookingCode,
                        ServiceId = sb.ServiceId,
                        ServiceName = sb.ServiceName,
                        BookingDate = sb.BookingDate,
                        ServiceDate = sb.ServiceDate,
                        Quantity = sb.Quantity,
                        UnitPrice = sb.UnitPrice,
                        TotalAmount = sb.TotalAmount,
                        Status = sb.Status,
                        SpecialRequests = sb.SpecialRequests,
                        CreatedAt = sb.CreatedAt,
                        CreatedBy = sb.CreatedBy
                    }).ToList(),
                    Invoices = booking.Invoices.Select(i => new InvoiceViewModel
                    {
                        InvoiceId = i.InvoiceId,
                        InvoiceNumber = i.InvoiceNumber,
                        InvoiceDate = i.InvoiceDate.ToDateTime(TimeOnly.MinValue),
                        Subtotal = i.Subtotal,
                        TaxAmount = i.TaxAmount ?? 0,
                        DiscountAmount = i.DiscountAmount ?? 0,
                        TotalAmount = i.TotalAmount,
                        Status = i.Status ?? "",
                        PaymentMethod = i.PaymentMethod ?? ""
                    }).ToList(),
                    CheckIns = booking.CheckIns.Select(c => new CheckInViewModel
                    {
                        CheckInId = c.CheckInId,
                        CheckInTime = c.CheckInTime ?? DateTime.Now,
                        CheckOutTime = c.CheckOutTime,
                        CheckedInBy = c.CheckedInByNavigation != null ? c.CheckedInByNavigation.Username : "",
                        CheckedOutBy = c.CheckedOutByNavigation != null ? c.CheckedOutByNavigation.Username : "",
                        Notes = c.Notes ?? ""
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting booking details by id {BookingId}", id);
                throw;
            }
        }

        public async Task<int> CreateAsync(CreateBookingDto dto, string createdByUser)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate room availability
                if (dto.RoomId.HasValue && !await IsRoomAvailableAsync(dto.RoomId.Value, dto.CheckInDate, dto.CheckOutDate))
                {
                    throw new InvalidOperationException("Phòng đã được đặt trong khoảng thời gian này");
                }

                // Calculate total amount
                var totalAmount = dto.RoomId.HasValue ? 
                    await CalculateTotalAmountAsync(dto.RoomId.Value, dto.CheckInDate, dto.CheckOutDate) : 0;

                // Generate booking code
                var bookingCode = await GenerateBookingCodeAsync();

                // Get created by user
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == createdByUser);

                var booking = new Booking
                {
                    BookingCode = bookingCode,
                    CustomerId = dto.CustomerId,
                    RoomId = dto.RoomId,
                    CheckInDate = DateOnly.FromDateTime(dto.CheckInDate),
                    CheckOutDate = DateOnly.FromDateTime(dto.CheckOutDate),
                    Adults = dto.Adults,
                    Children = dto.Children,
                    TotalAmount = totalAmount,
                    Status = "pending",
                    SpecialRequests = dto.SpecialRequests,
                    CreatedBy = user?.UserId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                // Update room status if room is assigned
                if (dto.RoomId.HasValue)
                {
                    var room = await _context.Rooms.FindAsync(dto.RoomId.Value);
                    if (room != null)
                    {
                        room.Status = "booked";
                        room.UpdatedAt = DateTime.Now;
                        _context.Rooms.Update(room);
                    }
                }

                // Log audit
                await LogAuditAsync(user?.UserId, "CREATE", "Bookings", booking.BookingId, null, booking);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Booking created successfully. BookingId: {BookingId}, Code: {BookingCode}", 
                    booking.BookingId, booking.BookingCode);

                return booking.BookingId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating booking");
                throw;
            }
        }

        public async Task<bool> ConfirmBookingAsync(int bookingId, int assignedRoomId, string performedBy)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var booking = await _context.Bookings.FindAsync(bookingId);
                if (booking == null) return false;

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == performedBy);
                var oldValues = new { BookingStatus = booking.Status, RoomId = booking.RoomId };

                booking.Status = "confirmed";
                booking.RoomId = assignedRoomId;
                booking.UpdatedAt = DateTime.Now;

                // Update room status
                var room = await _context.Rooms.FindAsync(assignedRoomId);
                if (room != null)
                {
                    room.Status = "booked";
                    room.UpdatedAt = DateTime.Now;
                    _context.Rooms.Update(room);
                }

                _context.Bookings.Update(booking);

                // Log audit
                await LogAuditAsync(user?.UserId, "CONFIRM", "Bookings", bookingId, oldValues, booking);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Booking confirmed successfully. BookingId: {BookingId}", bookingId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error confirming booking {BookingId}", bookingId);
                return false;
            }
        }

        public async Task<bool> AssignRoomAsync(int bookingId, int roomId, string performedBy)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var booking = await _context.Bookings.FindAsync(bookingId);
                if (booking == null) return false;

                // Check room availability
                if (!await IsRoomAvailableAsync(roomId, booking.CheckInDate.ToDateTime(TimeOnly.MinValue), 
                    booking.CheckOutDate.ToDateTime(TimeOnly.MinValue), bookingId))
                {
                    throw new InvalidOperationException("Phòng không có sẵn trong khoảng thời gian này");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == performedBy);
                var oldValues = new { booking.RoomId };

                booking.RoomId = roomId;
                booking.UpdatedAt = DateTime.Now;

                // Update room status
                var room = await _context.Rooms.FindAsync(roomId);
                if (room != null)
                {
                    room.Status = "booked";
                    room.UpdatedAt = DateTime.Now;
                    _context.Rooms.Update(room);
                }

                _context.Bookings.Update(booking);

                // Log audit
                await LogAuditAsync(user?.UserId, "ASSIGN_ROOM", "Bookings", bookingId, oldValues, booking);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Room assigned to booking successfully. BookingId: {BookingId}, RoomId: {RoomId}", 
                    bookingId, roomId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error assigning room to booking {BookingId}", bookingId);
                return false;
            }
        }

        public async Task<bool> CheckInAsync(int bookingId, string performedBy)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Room)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId);
                if (booking == null) return false;

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == performedBy);
                var oldValues = new { BookingStatus = booking.Status, RoomStatus = booking.Room?.Status };

                booking.Status = "checkedin";
                booking.UpdatedAt = DateTime.Now;

                // Update room status
                if (booking.Room != null)
                {
                    booking.Room.Status = "occupied";
                    booking.Room.UpdatedAt = DateTime.Now;
                    _context.Rooms.Update(booking.Room);
                }

                // Create check-in record
                var checkIn = new CheckIn
                {
                    BookingId = bookingId,
                    RoomId = booking.RoomId,
                    CheckInTime = DateTime.Now,
                    CheckedInBy = user?.UserId,
                    Notes = "Check-in completed"
                };

                _context.CheckIns.Add(checkIn);
                _context.Bookings.Update(booking);

                // Log audit
                await LogAuditAsync(user?.UserId, "CHECK_IN", "Bookings", bookingId, oldValues, booking);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Check-in completed successfully. BookingId: {BookingId}", bookingId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error checking in booking {BookingId}", bookingId);
                return false;
            }
        }

        public async Task<bool> CheckOutAsync(int bookingId, string performedBy)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Room)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId);
                if (booking == null) return false;

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == performedBy);
                var oldValues = new { BookingStatus = booking.Status, RoomStatus = booking.Room?.Status };

                booking.Status = "checkedout";
                booking.UpdatedAt = DateTime.Now;

                // Update room status
                if (booking.Room != null)
                {
                    booking.Room.Status = "cleaning";
                    booking.Room.UpdatedAt = DateTime.Now;
                    _context.Rooms.Update(booking.Room);
                }

                // Update check-in record
                var checkIn = await _context.CheckIns
                    .FirstOrDefaultAsync(c => c.BookingId == bookingId && c.CheckOutTime == null);
                if (checkIn != null)
                {
                    checkIn.CheckOutTime = DateTime.Now;
                    checkIn.CheckedOutBy = user?.UserId;
                    _context.CheckIns.Update(checkIn);
                }

                _context.Bookings.Update(booking);

                // Log audit
                await LogAuditAsync(user?.UserId, "CHECK_OUT", "Bookings", bookingId, oldValues, booking);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Check-out completed successfully. BookingId: {BookingId}", bookingId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error checking out booking {BookingId}", bookingId);
                return false;
            }
        }

        public async Task<bool> CancelBookingAsync(int bookingId, string performedBy)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Room)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId);
                if (booking == null) return false;

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == performedBy);
                var oldValues = new { BookingStatus = booking.Status, RoomStatus = booking.Room?.Status };

                booking.Status = "cancelled";
                booking.UpdatedAt = DateTime.Now;

                // Update room status
                if (booking.Room != null)
                {
                    booking.Room.Status = "available";
                    booking.Room.UpdatedAt = DateTime.Now;
                    _context.Rooms.Update(booking.Room);
                }

                _context.Bookings.Update(booking);

                // Log audit
                await LogAuditAsync(user?.UserId, "CANCEL", "Bookings", bookingId, oldValues, booking);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Booking cancelled successfully. BookingId: {BookingId}", bookingId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error cancelling booking {BookingId}", bookingId);
                return false;
            }
        }

        public async Task<IEnumerable<BookingViewModel>> GetByCustomerIdAsync(int customerId)
        {
            try
            {
                var bookings = await _context.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                    .Where(b => b.CustomerId == customerId)
                    .OrderByDescending(b => b.CreatedAt)
                    .Select(b => new BookingViewModel
                    {
                        BookingId = b.BookingId,
                        BookingCode = b.BookingCode,
                        CustomerId = b.CustomerId ?? 0,
                        CustomerName = b.Customer != null ? $"{b.Customer.FirstName} {b.Customer.LastName}" : "",
                        RoomId = b.RoomId,
                        RoomNumber = b.Room != null ? b.Room.RoomNumber : "",
                        RoomType = b.Room != null && b.Room.RoomType != null ? b.Room.RoomType.TypeName : "",
                        CheckInDate = b.CheckInDate.ToDateTime(TimeOnly.MinValue),
                        CheckOutDate = b.CheckOutDate.ToDateTime(TimeOnly.MinValue),
                        Adults = b.Adults ?? 1,
                        Children = b.Children ?? 0,
                        TotalAmount = b.TotalAmount ?? 0,
                        DepositAmount = b.DepositAmount,
                        Status = b.Status ?? "",
                        SpecialRequests = b.SpecialRequests ?? "",
                        CreatedAt = b.CreatedAt ?? DateTime.Now,
                        UpdatedAt = b.UpdatedAt,
                        CreatedBy = b.CreatedByNavigation != null ? b.CreatedByNavigation.Username : ""
                    })
                    .ToListAsync();

                return bookings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bookings by customer id {CustomerId}", customerId);
                throw;
            }
        }

        public async Task<IEnumerable<BookingViewModel>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var start = DateOnly.FromDateTime(startDate);
                var end = DateOnly.FromDateTime(endDate);

                var bookings = await _context.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                    .Where(b => b.CheckInDate >= start && b.CheckInDate <= end)
                    .OrderByDescending(b => b.CreatedAt)
                    .Select(b => new BookingViewModel
                    {
                        BookingId = b.BookingId,
                        BookingCode = b.BookingCode,
                        CustomerId = b.CustomerId ?? 0,
                        CustomerName = b.Customer != null ? $"{b.Customer.FirstName} {b.Customer.LastName}" : "",
                        RoomId = b.RoomId,
                        RoomNumber = b.Room != null ? b.Room.RoomNumber : "",
                        RoomType = b.Room != null && b.Room.RoomType != null ? b.Room.RoomType.TypeName : "",
                        CheckInDate = b.CheckInDate.ToDateTime(TimeOnly.MinValue),
                        CheckOutDate = b.CheckOutDate.ToDateTime(TimeOnly.MinValue),
                        Adults = b.Adults ?? 1,
                        Children = b.Children ?? 0,
                        TotalAmount = b.TotalAmount ?? 0,
                        DepositAmount = b.DepositAmount,
                        Status = b.Status ?? "",
                        SpecialRequests = b.SpecialRequests ?? "",
                        CreatedAt = b.CreatedAt ?? DateTime.Now,
                        UpdatedAt = b.UpdatedAt,
                        CreatedBy = b.CreatedByNavigation != null ? b.CreatedByNavigation.Username : ""
                    })
                    .ToListAsync();

                return bookings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bookings by date range {StartDate} - {EndDate}", startDate, endDate);
                throw;
            }
        }

        public async Task<IEnumerable<BookingViewModel>> GetByStatusAsync(string status)
        {
            try
            {
                var bookings = await _context.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                    .Where(b => b.Status == status)
                    .OrderByDescending(b => b.CreatedAt)
                    .Select(b => new BookingViewModel
                    {
                        BookingId = b.BookingId,
                        BookingCode = b.BookingCode,
                        CustomerId = b.CustomerId ?? 0,
                        CustomerName = b.Customer != null ? $"{b.Customer.FirstName} {b.Customer.LastName}" : "",
                        RoomId = b.RoomId,
                        RoomNumber = b.Room != null ? b.Room.RoomNumber : "",
                        RoomType = b.Room != null && b.Room.RoomType != null ? b.Room.RoomType.TypeName : "",
                        CheckInDate = b.CheckInDate.ToDateTime(TimeOnly.MinValue),
                        CheckOutDate = b.CheckOutDate.ToDateTime(TimeOnly.MinValue),
                        Adults = b.Adults ?? 1,
                        Children = b.Children ?? 0,
                        TotalAmount = b.TotalAmount ?? 0,
                        DepositAmount = b.DepositAmount,
                        Status = b.Status ?? "",
                        SpecialRequests = b.SpecialRequests ?? "",
                        CreatedAt = b.CreatedAt ?? DateTime.Now,
                        UpdatedAt = b.UpdatedAt,
                        CreatedBy = b.CreatedByNavigation != null ? b.CreatedByNavigation.Username : ""
                    })
                    .ToListAsync();

                return bookings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bookings by status {Status}", status);
                throw;
            }
        }

        public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeBookingId = null)
        {
            try
            {
                var start = DateOnly.FromDateTime(checkIn);
                var end = DateOnly.FromDateTime(checkOut);

                var conflictingBookings = await _context.Bookings
                    .Where(b => b.RoomId == roomId && 
                               b.BookingId != excludeBookingId &&
                               b.Status != "cancelled" && 
                               b.Status != "checkedout" &&
                               ((b.CheckInDate <= start && b.CheckOutDate > start) ||
                                (b.CheckInDate < end && b.CheckOutDate >= end) ||
                                (b.CheckInDate >= start && b.CheckOutDate <= end)))
                    .AnyAsync();

                return !conflictingBookings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking room availability for room {RoomId}", roomId);
                throw;
            }
        }

        public async Task<decimal> CalculateTotalAmountAsync(int roomId, DateTime checkIn, DateTime checkOut)
        {
            try
            {
                var room = await _context.Rooms.FindAsync(roomId);
                if (room == null) return 0;

                var nights = (checkOut - checkIn).Days;
                return room.Price * nights;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total amount for room {RoomId}", roomId);
                throw;
            }
        }

        private async Task<string> GenerateBookingCodeAsync()
        {
            var today = DateTime.Today;
            var prefix = $"BKG{today:yyyyMMdd}";
            var lastBooking = await _context.Bookings
                .Where(b => b.BookingCode.StartsWith(prefix))
                .OrderByDescending(b => b.BookingCode)
                .FirstOrDefaultAsync();

            if (lastBooking == null)
            {
                return $"{prefix}001";
            }

            var lastNumber = int.Parse(lastBooking.BookingCode.Substring(prefix.Length));
            return $"{prefix}{(lastNumber + 1):D3}";
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
                    IpAddress = "127.0.0.1", // In real app, get from HttpContext
                    UserAgent = "System", // In real app, get from HttpContext
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
