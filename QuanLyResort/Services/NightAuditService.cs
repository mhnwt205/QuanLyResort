using Microsoft.EntityFrameworkCore;
using QuanLyResort.Models;
using QuanLyResort.Services.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QuanLyResort.Services
{
    public class NightAuditService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NightAuditService> _logger;
        private Timer? _timer;
        private readonly TimeSpan _auditTime = new TimeSpan(23, 50, 0); // 11:50 PM

        public NightAuditService(IServiceProvider serviceProvider, ILogger<NightAuditService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Night Audit Service started.");
            
            // Calculate time until next audit
            var now = DateTime.Now.TimeOfDay;
            var timeUntilAudit = _auditTime - now;
            if (timeUntilAudit <= TimeSpan.Zero)
            {
                timeUntilAudit = timeUntilAudit.Add(TimeSpan.FromDays(1));
            }

            _timer = new Timer(DoWork, null, timeUntilAudit, TimeSpan.FromDays(1));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Night Audit Service stopped.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private async void DoWork(object? state)
        {
            try
            {
                _logger.LogInformation("Starting Night Audit at {Time}", DateTime.Now);
                await RunNightAuditAsync();
                _logger.LogInformation("Night Audit completed at {Time}", DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Night Audit");
            }
        }

        private async Task RunNightAuditAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ResortDbContext>();
            var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();

            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var auditDate = DateTime.Today;
                var auditLog = new AuditLog
                {
                    UserId = null, // System audit
                    Action = "NIGHT_AUDIT",
                    TableName = "System",
                    RecordId = 0,
                    OldValues = null,
                    NewValues = System.Text.Json.JsonSerializer.Serialize(new { AuditDate = auditDate }),
                    IpAddress = "127.0.0.1",
                    UserAgent = "NightAuditService",
                    CreatedAt = DateTime.Now
                };

                context.AuditLogs.Add(auditLog);

                // 1. Check for bookings not checked in by check-in date
                var overdueCheckIns = await context.Bookings
                    .Where(b => b.CheckInDate < DateOnly.FromDateTime(auditDate) && 
                               b.Status == "pending")
                    .ToListAsync();

                foreach (var booking in overdueCheckIns)
                {
                    booking.Status = "no-show";
                    booking.UpdatedAt = DateTime.Now;
                    context.Bookings.Update(booking);

                    // Log the action
                    var noShowLog = new AuditLog
                    {
                        UserId = null,
                        Action = "NO_SHOW",
                        TableName = "Bookings",
                        RecordId = booking.BookingId,
                        OldValues = "pending",
                        NewValues = "no-show",
                        IpAddress = "127.0.0.1",
                        UserAgent = "NightAuditService",
                        CreatedAt = DateTime.Now
                    };
                    context.AuditLogs.Add(noShowLog);
                }

                // 2. Check for rooms still occupied after check-out date
                var overdueCheckOuts = await context.Bookings
                    .Where(b => b.CheckOutDate < DateOnly.FromDateTime(auditDate) && 
                               b.Status == "checkedin")
                    .Include(b => b.Room)
                    .ToListAsync();

                foreach (var booking in overdueCheckOuts)
                {
                    booking.Status = "overdue-checkout";
                    booking.UpdatedAt = DateTime.Now;
                    context.Bookings.Update(booking);

                    if (booking.Room != null)
                    {
                        booking.Room.Status = "occupied-overdue";
                        booking.Room.UpdatedAt = DateTime.Now;
                        context.Rooms.Update(booking.Room);
                    }

                    // Log the action
                    var overdueLog = new AuditLog
                    {
                        UserId = null,
                        Action = "OVERDUE_CHECKOUT",
                        TableName = "Bookings",
                        RecordId = booking.BookingId,
                        OldValues = "checkedin",
                        NewValues = "overdue-checkout",
                        IpAddress = "127.0.0.1",
                        UserAgent = "NightAuditService",
                        CreatedAt = DateTime.Now
                    };
                    context.AuditLogs.Add(overdueLog);
                }

                // 3. Generate daily revenue report
                var dailyRevenue = await context.Invoices
                    .Where(i => i.InvoiceDate == DateOnly.FromDateTime(auditDate) && i.Status == "paid")
                    .SumAsync(i => i.TotalAmount);

                var roomRevenue = await context.Invoices
                    .Where(i => i.InvoiceDate == DateOnly.FromDateTime(auditDate) && i.Status == "paid")
                    .SelectMany(i => i.InvoiceItems)
                    .Where(ii => ii.ItemType == "room")
                    .SumAsync(ii => ii.TotalPrice);

                var serviceRevenue = await context.Invoices
                    .Where(i => i.InvoiceDate == DateOnly.FromDateTime(auditDate) && i.Status == "paid")
                    .SelectMany(i => i.InvoiceItems)
                    .Where(ii => ii.ItemType == "service")
                    .SumAsync(ii => ii.TotalPrice);

                // 4. Check for low stock items
                var lowStockItems = await context.Inventories
                    .Include(i => i.Item)
                    .Where(i => i.QuantityOnHand <= i.MinStockLevel)
                    .ToListAsync();

                foreach (var item in lowStockItems)
                {
                    var lowStockLog = new AuditLog
                    {
                        UserId = null,
                        Action = "LOW_STOCK_ALERT",
                        TableName = "Inventories",
                        RecordId = item.InventoryId,
                        OldValues = null,
                        NewValues = System.Text.Json.JsonSerializer.Serialize(new 
                        { 
                            ItemName = item.Item?.ItemName,
                            QuantityOnHand = item.QuantityOnHand,
                            MinStockLevel = item.MinStockLevel
                        }),
                        IpAddress = "127.0.0.1",
                        UserAgent = "NightAuditService",
                        CreatedAt = DateTime.Now
                    };
                    context.AuditLogs.Add(lowStockLog);
                }

                // 5. Finalize pending invoices for checked-out guests
                var pendingInvoices = await context.Invoices
                    .Where(i => i.Status == "draft" && 
                               i.Booking != null && 
                               i.Booking.Status == "checkedout")
                    .ToListAsync();

                foreach (var invoice in pendingInvoices)
                {
                    invoice.Status = "pending_payment";
                    invoice.UpdatedAt = DateTime.Now;
                    context.Invoices.Update(invoice);

                    var invoiceLog = new AuditLog
                    {
                        UserId = null,
                        Action = "FINALIZE_INVOICE",
                        TableName = "Invoices",
                        RecordId = invoice.InvoiceId,
                        OldValues = "draft",
                        NewValues = "pending_payment",
                        IpAddress = "127.0.0.1",
                        UserAgent = "NightAuditService",
                        CreatedAt = DateTime.Now
                    };
                    context.AuditLogs.Add(invoiceLog);
                }

                // 6. Update room statuses for cleaning
                var roomsToClean = await context.Rooms
                    .Where(r => r.Status == "cleaning")
                    .ToListAsync();

                foreach (var room in roomsToClean)
                {
                    room.Status = "available";
                    room.UpdatedAt = DateTime.Now;
                    context.Rooms.Update(room);
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Night Audit Summary: " +
                    "Overdue Check-ins: {OverdueCheckIns}, " +
                    "Overdue Check-outs: {OverdueCheckOuts}, " +
                    "Daily Revenue: {DailyRevenue}, " +
                    "Low Stock Items: {LowStockItems}, " +
                    "Finalized Invoices: {FinalizedInvoices}, " +
                    "Rooms Cleaned: {RoomsCleaned}",
                    overdueCheckIns.Count,
                    overdueCheckOuts.Count,
                    dailyRevenue,
                    lowStockItems.Count,
                    pendingInvoices.Count,
                    roomsToClean.Count);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during Night Audit transaction");
                throw;
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
