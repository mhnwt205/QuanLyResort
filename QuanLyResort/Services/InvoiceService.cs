using Microsoft.EntityFrameworkCore;
using QuanLyResort.Models;
using QuanLyResort.Services.Interfaces;
using QuanLyResort.ViewModels;

namespace QuanLyResort.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly ResortDbContext _context;
        private readonly ILogger<InvoiceService> _logger;

        public InvoiceService(ResortDbContext context, ILogger<InvoiceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<InvoiceDto>> GetAllAsync()
        {
            try
            {
                var invoices = await _context.Invoices
                    .Include(i => i.Customer)
                    .Include(i => i.Booking)
                    .Include(i => i.CreatedByNavigation)
                    .OrderByDescending(i => i.CreatedAt)
                    .Select(i => new InvoiceDto
                    {
                        InvoiceId = i.InvoiceId,
                        InvoiceNumber = i.InvoiceNumber,
                        CustomerId = i.CustomerId,
                        CustomerName = i.Customer != null ? $"{i.Customer.FirstName} {i.Customer.LastName}" : "",
                        BookingId = i.BookingId,
                        BookingCode = i.Booking.BookingCode,
                        InvoiceDate = i.InvoiceDate.ToDateTime(TimeOnly.MinValue),
                        DueDate = i.DueDate.HasValue ? i.DueDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                        Subtotal = i.Subtotal,
                        TaxAmount = i.TaxAmount ?? 0,
                        DiscountAmount = i.DiscountAmount ?? 0,
                        TotalAmount = i.TotalAmount,
                        Status = i.Status ?? "",
                        PaymentMethod = i.PaymentMethod ?? "",
                        Notes = i.Notes ?? "",
                        CreatedAt = i.CreatedAt ?? DateTime.Now,
                        CreatedBy = i.CreatedByNavigation != null ? i.CreatedByNavigation.Username : ""
                    })
                    .ToListAsync();

                return invoices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all invoices");
                throw;
            }
        }

        public async Task<InvoiceDto?> GetByIdAsync(int id)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.Customer)
                    .Include(i => i.Booking)
                    .Include(i => i.CreatedByNavigation)
                    .Include(i => i.InvoiceItems)
                    .Include(i => i.Payments)
                    .FirstOrDefaultAsync(i => i.InvoiceId == id);

                if (invoice == null) return null;

                return new InvoiceDto
                {
                    InvoiceId = invoice.InvoiceId,
                    InvoiceNumber = invoice.InvoiceNumber,
                    CustomerId = invoice.CustomerId,
                    CustomerName = invoice.Customer != null ? $"{invoice.Customer.FirstName} {invoice.Customer.LastName}" : "",
                    BookingId = invoice.BookingId,
                    BookingCode = invoice.Booking != null ? invoice.Booking.BookingCode : "",
                    InvoiceDate = invoice.InvoiceDate.ToDateTime(TimeOnly.MinValue),
                    DueDate = invoice.DueDate?.ToDateTime(TimeOnly.MinValue),
                    Subtotal = invoice.Subtotal,
                    TaxAmount = invoice.TaxAmount ?? 0,
                    DiscountAmount = invoice.DiscountAmount ?? 0,
                    TotalAmount = invoice.TotalAmount,
                    Status = invoice.Status ?? "",
                    PaymentMethod = invoice.PaymentMethod ?? "",
                    Notes = invoice.Notes ?? "",
                    CreatedAt = invoice.CreatedAt ?? DateTime.Now,
                    CreatedBy = invoice.CreatedByNavigation != null ? invoice.CreatedByNavigation.Username : "",
                    InvoiceItems = invoice.InvoiceItems.Select(item => new InvoiceItemViewModel
                    {
                        ItemId = item.ItemId,
                        InvoiceId = item.InvoiceId ?? 0,
                        ItemName = item.ItemName,
                        ItemType = item.ItemType,
                        Description = item.Description ?? "",
                        Quantity = item.Quantity ?? 0,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice,
                        CreatedAt = item.CreatedAt ?? DateTime.Now
                    }).ToList(),
                    Payments = invoice.Payments.Select(p => new PaymentViewModel
                    {
                        PaymentId = p.PaymentId,
                        PaymentNumber = p.PaymentNumber,
                        InvoiceId = p.InvoiceId ?? 0,
                        PaymentDate = p.PaymentDate.ToDateTime(TimeOnly.MinValue),
                        Amount = p.Amount,
                        PaymentMethod = p.PaymentMethod ?? "",
                        ReferenceNumber = p.ReferenceNumber ?? "",
                        Notes = p.Notes ?? "",
                        ProcessedBy = p.ProcessedByNavigation != null ? p.ProcessedByNavigation.Username : "",
                        CreatedAt = p.CreatedAt ?? DateTime.Now
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting invoice by id {InvoiceId}", id);
                throw;
            }
        }

        public async Task<int> CreateAsync(CreateInvoiceDto dto, string createdByUser)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == createdByUser);
                var invoiceNumber = await GenerateInvoiceNumberAsync();

                var invoice = new Invoice
                {
                    InvoiceNumber = invoiceNumber,
                    CustomerId = dto.CustomerId,
                    BookingId = dto.BookingId,
                    InvoiceDate = DateOnly.FromDateTime(dto.InvoiceDate),
                    DueDate = dto.DueDate.HasValue ? DateOnly.FromDateTime(dto.DueDate.Value) : null,
                    Subtotal = dto.Subtotal,
                    TaxAmount = dto.TaxAmount,
                    DiscountAmount = dto.DiscountAmount,
                    TotalAmount = dto.TotalAmount,
                    Status = "draft",
                    PaymentMethod = dto.PaymentMethod,
                    Notes = dto.Notes,
                    CreatedBy = user?.UserId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                // Log audit
                await LogAuditAsync(user?.UserId, "CREATE", "Invoices", invoice.InvoiceId, null, invoice);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Invoice created successfully. InvoiceId: {InvoiceId}, Number: {InvoiceNumber}", 
                    invoice.InvoiceId, invoice.InvoiceNumber);

                return invoice.InvoiceId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating invoice");
                throw;
            }
        }

        public async Task<int> CreateFromBookingAsync(int bookingId, decimal taxRate = 0.1m, decimal discount = 0, string createdByUser = "system")
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId);

                if (booking == null)
                {
                    throw new InvalidOperationException("Booking not found");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == createdByUser);
                var invoiceNumber = await GenerateInvoiceNumberAsync();

                // Calculate room cost
                var nights = (booking.CheckOutDate.ToDateTime(TimeOnly.MinValue) - booking.CheckInDate.ToDateTime(TimeOnly.MinValue)).Days;
                var roomCost = booking.Room != null ? booking.Room.Price * nights : 0;

                // Get service costs
                var serviceBookings = await _context.ServiceBookings
                    .Include(sb => sb.Service)
                    .Where(sb => sb.CustomerId == booking.CustomerId &&
                               sb.ServiceDate >= booking.CheckInDate &&
                               sb.ServiceDate <= booking.CheckOutDate)
                    .ToListAsync();

                var serviceCost = serviceBookings.Sum(sb => sb.TotalAmount ?? 0);

                // Calculate totals
                var subtotal = roomCost + serviceCost;
                var taxAmount = subtotal * taxRate;
                var totalAmount = subtotal + taxAmount - discount;

                var invoice = new Invoice
                {
                    InvoiceNumber = invoiceNumber,
                    CustomerId = booking.CustomerId,
                    BookingId = bookingId,
                    InvoiceDate = DateOnly.FromDateTime(DateTime.Today),
                    DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
                    Subtotal = subtotal,
                    TaxAmount = taxAmount,
                    DiscountAmount = discount,
                    TotalAmount = totalAmount,
                    Status = "draft",
                    PaymentMethod = "cash",
                    Notes = $"Invoice for booking {booking.BookingCode}",
                    CreatedBy = user?.UserId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                // Create invoice items for room
                if (roomCost > 0 && booking.Room != null)
                {
                    var roomItem = new InvoiceItem
                    {
                        InvoiceId = invoice.InvoiceId,
                        ItemName = $"Phòng {booking.Room.RoomNumber} ({booking.Room.RoomType?.TypeName})",
                        ItemType = "room",
                        Description = $"Đặt phòng từ {booking.CheckInDate:dd/MM/yyyy} đến {booking.CheckOutDate:dd/MM/yyyy}",
                        Quantity = nights,
                        UnitPrice = booking.Room.Price,
                        TotalPrice = roomCost,
                        CreatedAt = DateTime.Now
                    };
                    _context.InvoiceItems.Add(roomItem);
                }

                // Create invoice items for services
                foreach (var serviceBooking in serviceBookings)
                {
                    var serviceItem = new InvoiceItem
                    {
                        InvoiceId = invoice.InvoiceId,
                        ItemName = serviceBooking.Service?.ServiceName ?? "Dịch vụ",
                        ItemType = "service",
                        Description = serviceBooking.SpecialRequests,
                        Quantity = serviceBooking.Quantity ?? 1,
                        UnitPrice = serviceBooking.UnitPrice ?? 0,
                        TotalPrice = serviceBooking.TotalAmount ?? 0,
                        CreatedAt = DateTime.Now
                    };
                    _context.InvoiceItems.Add(serviceItem);
                }

                // Log audit
                await LogAuditAsync(user?.UserId, "CREATE_FROM_BOOKING", "Invoices", invoice.InvoiceId, null, invoice);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Invoice created from booking successfully. InvoiceId: {InvoiceId}, BookingId: {BookingId}", 
                    invoice.InvoiceId, bookingId);

                return invoice.InvoiceId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating invoice from booking {BookingId}", bookingId);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(int id, CreateInvoiceDto dto, string updatedByUser)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var invoice = await _context.Invoices.FindAsync(id);
                if (invoice == null) return false;

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == updatedByUser);
                var oldValues = new { invoice.Subtotal, invoice.TaxAmount, invoice.DiscountAmount, invoice.TotalAmount };

                invoice.CustomerId = dto.CustomerId;
                invoice.BookingId = dto.BookingId;
                invoice.InvoiceDate = DateOnly.FromDateTime(dto.InvoiceDate);
                invoice.DueDate = dto.DueDate.HasValue ? DateOnly.FromDateTime(dto.DueDate.Value) : null;
                invoice.Subtotal = dto.Subtotal;
                invoice.TaxAmount = dto.TaxAmount;
                invoice.DiscountAmount = dto.DiscountAmount;
                invoice.TotalAmount = dto.TotalAmount;
                invoice.PaymentMethod = dto.PaymentMethod;
                invoice.Notes = dto.Notes;
                invoice.UpdatedAt = DateTime.Now;

                _context.Invoices.Update(invoice);

                // Log audit
                await LogAuditAsync(user?.UserId, "UPDATE", "Invoices", id, oldValues, invoice);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Invoice updated successfully. InvoiceId: {InvoiceId}", id);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating invoice {InvoiceId}", id);
                return false;
            }
        }

        public async Task<bool> ApproveAsync(int id, string approvedByUser)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var invoice = await _context.Invoices.FindAsync(id);
                if (invoice == null) return false;

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == approvedByUser);
                var oldValues = new { invoice.Status };

                invoice.Status = "approved";
                invoice.UpdatedAt = DateTime.Now;

                _context.Invoices.Update(invoice);

                // Log audit
                await LogAuditAsync(user?.UserId, "APPROVE", "Invoices", id, oldValues, invoice);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Invoice approved successfully. InvoiceId: {InvoiceId}", id);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error approving invoice {InvoiceId}", id);
                return false;
            }
        }

        public async Task<bool> CancelAsync(int id, string cancelledByUser)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var invoice = await _context.Invoices.FindAsync(id);
                if (invoice == null) return false;

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == cancelledByUser);
                var oldValues = new { invoice.Status };

                invoice.Status = "cancelled";
                invoice.UpdatedAt = DateTime.Now;

                _context.Invoices.Update(invoice);

                // Log audit
                await LogAuditAsync(user?.UserId, "CANCEL", "Invoices", id, oldValues, invoice);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Invoice cancelled successfully. InvoiceId: {InvoiceId}", id);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error cancelling invoice {InvoiceId}", id);
                return false;
            }
        }

        public async Task<decimal> GetRemainingAmountAsync(int invoiceId)
        {
            try
            {
                var invoice = await _context.Invoices.FindAsync(invoiceId);
                if (invoice == null) return 0;

                var totalPaid = await _context.Payments
                    .Where(p => p.InvoiceId == invoiceId)
                    .SumAsync(p => p.Amount);

                return invoice.TotalAmount - totalPaid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting remaining amount for invoice {InvoiceId}", invoiceId);
                throw;
            }
        }

        public async Task<bool> IsFullyPaidAsync(int invoiceId)
        {
            try
            {
                var remainingAmount = await GetRemainingAmountAsync(invoiceId);
                return remainingAmount <= 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if invoice is fully paid {InvoiceId}", invoiceId);
                throw;
            }
        }

        public async Task<IEnumerable<InvoiceDto>> GetByCustomerIdAsync(int customerId)
        {
            try
            {
                var invoices = await _context.Invoices
                    .Include(i => i.Customer)
                    .Include(i => i.Booking)
                    .Where(i => i.CustomerId == customerId)
                    .OrderByDescending(i => i.CreatedAt)
                    .Select(i => new InvoiceDto
                    {
                        InvoiceId = i.InvoiceId,
                        InvoiceNumber = i.InvoiceNumber,
                        CustomerId = i.CustomerId,
                        CustomerName = i.Customer != null ? $"{i.Customer.FirstName} {i.Customer.LastName}" : "",
                        BookingId = i.BookingId,
                        BookingCode = i.Booking.BookingCode,
                        InvoiceDate = i.InvoiceDate.ToDateTime(TimeOnly.MinValue),
                        DueDate = i.DueDate.HasValue ? i.DueDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                        Subtotal = i.Subtotal,
                        TaxAmount = i.TaxAmount ?? 0,
                        DiscountAmount = i.DiscountAmount ?? 0,
                        TotalAmount = i.TotalAmount,
                        Status = i.Status ?? "",
                        PaymentMethod = i.PaymentMethod ?? "",
                        Notes = i.Notes ?? "",
                        CreatedAt = i.CreatedAt ?? DateTime.Now,
                        CreatedBy = i.CreatedByNavigation != null ? i.CreatedByNavigation.Username : ""
                    })
                    .ToListAsync();

                return invoices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting invoices by customer id {CustomerId}", customerId);
                throw;
            }
        }

        public async Task<IEnumerable<InvoiceDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var start = DateOnly.FromDateTime(startDate);
                var end = DateOnly.FromDateTime(endDate);

                var invoices = await _context.Invoices
                    .Include(i => i.Customer)
                    .Include(i => i.Booking)
                    .Where(i => i.InvoiceDate >= start && i.InvoiceDate <= end)
                    .OrderByDescending(i => i.CreatedAt)
                    .Select(i => new InvoiceDto
                    {
                        InvoiceId = i.InvoiceId,
                        InvoiceNumber = i.InvoiceNumber,
                        CustomerId = i.CustomerId,
                        CustomerName = i.Customer != null ? $"{i.Customer.FirstName} {i.Customer.LastName}" : "",
                        BookingId = i.BookingId,
                        BookingCode = i.Booking.BookingCode,
                        InvoiceDate = i.InvoiceDate.ToDateTime(TimeOnly.MinValue),
                        DueDate = i.DueDate.HasValue ? i.DueDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                        Subtotal = i.Subtotal,
                        TaxAmount = i.TaxAmount ?? 0,
                        DiscountAmount = i.DiscountAmount ?? 0,
                        TotalAmount = i.TotalAmount,
                        Status = i.Status ?? "",
                        PaymentMethod = i.PaymentMethod ?? "",
                        Notes = i.Notes ?? "",
                        CreatedAt = i.CreatedAt ?? DateTime.Now,
                        CreatedBy = i.CreatedByNavigation != null ? i.CreatedByNavigation.Username : ""
                    })
                    .ToListAsync();

                return invoices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting invoices by date range {StartDate} - {EndDate}", startDate, endDate);
                throw;
            }
        }

        public async Task<IEnumerable<InvoiceDto>> GetByStatusAsync(string status)
        {
            try
            {
                var invoices = await _context.Invoices
                    .Include(i => i.Customer)
                    .Include(i => i.Booking)
                    .Where(i => i.Status == status)
                    .OrderByDescending(i => i.CreatedAt)
                    .Select(i => new InvoiceDto
                    {
                        InvoiceId = i.InvoiceId,
                        InvoiceNumber = i.InvoiceNumber,
                        CustomerId = i.CustomerId,
                        CustomerName = i.Customer != null ? $"{i.Customer.FirstName} {i.Customer.LastName}" : "",
                        BookingId = i.BookingId,
                        BookingCode = i.Booking.BookingCode,
                        InvoiceDate = i.InvoiceDate.ToDateTime(TimeOnly.MinValue),
                        DueDate = i.DueDate.HasValue ? i.DueDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                        Subtotal = i.Subtotal,
                        TaxAmount = i.TaxAmount ?? 0,
                        DiscountAmount = i.DiscountAmount ?? 0,
                        TotalAmount = i.TotalAmount,
                        Status = i.Status ?? "",
                        PaymentMethod = i.PaymentMethod ?? "",
                        Notes = i.Notes ?? "",
                        CreatedAt = i.CreatedAt ?? DateTime.Now,
                        CreatedBy = i.CreatedByNavigation != null ? i.CreatedByNavigation.Username : ""
                    })
                    .ToListAsync();

                return invoices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting invoices by status {Status}", status);
                throw;
            }
        }

        public async Task<string> GenerateInvoiceNumberAsync()
        {
            try
            {
                var today = DateTime.Today;
                var prefix = $"INV{today:yyyyMMdd}";
                var lastInvoice = await _context.Invoices
                    .Where(i => i.InvoiceNumber.StartsWith(prefix))
                    .OrderByDescending(i => i.InvoiceNumber)
                    .FirstOrDefaultAsync();

                if (lastInvoice == null)
                {
                    return $"{prefix}001";
                }

                var lastNumber = int.Parse(lastInvoice.InvoiceNumber.Substring(prefix.Length));
                return $"{prefix}{(lastNumber + 1):D3}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice number");
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
