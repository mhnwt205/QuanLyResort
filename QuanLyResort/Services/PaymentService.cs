using Microsoft.EntityFrameworkCore;
using QuanLyResort.Models;
using QuanLyResort.Services.Interfaces;
using QuanLyResort.ViewModels;

namespace QuanLyResort.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ResortDbContext _context;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(ResortDbContext context, ILogger<PaymentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<PaymentViewModel>> GetAllAsync()
        {
            try
            {
                var payments = await _context.Payments
                    .Include(p => p.Invoice)
                    .Include(p => p.ProcessedByNavigation)
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new PaymentViewModel
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
                    })
                    .ToListAsync();

                return payments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all payments");
                throw;
            }
        }

        public async Task<PaymentViewModel?> GetByIdAsync(int id)
        {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Invoice)
                    .Include(p => p.ProcessedByNavigation)
                    .FirstOrDefaultAsync(p => p.PaymentId == id);

                if (payment == null) return null;

                return new PaymentViewModel
                {
                    PaymentId = payment.PaymentId,
                    PaymentNumber = payment.PaymentNumber,
                    InvoiceId = payment.InvoiceId ?? 0,
                    PaymentDate = payment.PaymentDate.ToDateTime(TimeOnly.MinValue),
                    Amount = payment.Amount,
                    PaymentMethod = payment.PaymentMethod ?? "",
                    ReferenceNumber = payment.ReferenceNumber ?? "",
                    Notes = payment.Notes ?? "",
                    ProcessedBy = payment.ProcessedByNavigation != null ? payment.ProcessedByNavigation.Username : "",
                    CreatedAt = payment.CreatedAt ?? DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment by id {PaymentId}", id);
                throw;
            }
        }

        public async Task<int> ProcessPaymentAsync(CreatePaymentDto dto, string processedByUser)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate payment
                if (!await ValidatePaymentAsync(dto))
                {
                    throw new InvalidOperationException("Payment validation failed");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == processedByUser);
                var paymentNumber = await GeneratePaymentNumberAsync();

                var payment = new Payment
                {
                    PaymentNumber = paymentNumber,
                    InvoiceId = dto.InvoiceId,
                    PaymentDate = DateOnly.FromDateTime(dto.PaymentDate),
                    Amount = dto.Amount,
                    PaymentMethod = dto.PaymentMethod,
                    ReferenceNumber = dto.ReferenceNumber,
                    Notes = dto.Notes,
                    ProcessedBy = user?.UserId,
                    CreatedAt = DateTime.Now
                };

                _context.Payments.Add(payment);

                // Check if invoice is fully paid
                var invoice = await _context.Invoices.FindAsync(dto.InvoiceId);
                if (invoice != null)
                {
                    var totalPaid = await _context.Payments
                        .Where(p => p.InvoiceId == dto.InvoiceId)
                        .SumAsync(p => p.Amount);

                    if (totalPaid >= invoice.TotalAmount)
                    {
                        invoice.Status = "paid";
                        invoice.UpdatedAt = DateTime.Now;
                        _context.Invoices.Update(invoice);
                    }
                }

                // Log audit
                await LogAuditAsync(user?.UserId, "PROCESS_PAYMENT", "Payments", payment.PaymentId, null, payment);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Payment processed successfully. PaymentId: {PaymentId}, Amount: {Amount}", 
                    payment.PaymentId, payment.Amount);

                return payment.PaymentId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error processing payment");
                throw;
            }
        }

        public async Task<bool> RefundPaymentAsync(int paymentId, decimal amount, string reason, string processedByUser)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var originalPayment = await _context.Payments.FindAsync(paymentId);
                if (originalPayment == null) return false;

                if (amount > originalPayment.Amount)
                {
                    throw new InvalidOperationException("Refund amount cannot exceed original payment amount");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == processedByUser);
                var refundNumber = await GeneratePaymentNumberAsync();

                // Create refund payment (negative amount)
                var refundPayment = new Payment
                {
                    PaymentNumber = refundNumber,
                    InvoiceId = originalPayment.InvoiceId,
                    PaymentDate = DateOnly.FromDateTime(DateTime.Today),
                    Amount = -amount, // Negative amount for refund
                    PaymentMethod = originalPayment.PaymentMethod,
                    ReferenceNumber = $"REFUND-{originalPayment.ReferenceNumber}",
                    Notes = $"Refund: {reason}",
                    ProcessedBy = user?.UserId,
                    CreatedAt = DateTime.Now
                };

                _context.Payments.Add(refundPayment);

                // Update invoice status if needed
                var invoice = await _context.Invoices.FindAsync(originalPayment.InvoiceId);
                if (invoice != null)
                {
                    var totalPaid = await _context.Payments
                        .Where(p => p.InvoiceId == originalPayment.InvoiceId)
                        .SumAsync(p => p.Amount);

                    if (totalPaid < invoice.TotalAmount)
                    {
                        invoice.Status = "partial";
                        invoice.UpdatedAt = DateTime.Now;
                        _context.Invoices.Update(invoice);
                    }
                }

                // Log audit
                await LogAuditAsync(user?.UserId, "REFUND_PAYMENT", "Payments", refundPayment.PaymentId, 
                    new { OriginalPaymentId = paymentId, RefundAmount = amount }, refundPayment);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Payment refunded successfully. PaymentId: {PaymentId}, RefundAmount: {Amount}", 
                    paymentId, amount);

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error refunding payment {PaymentId}", paymentId);
                return false;
            }
        }

        public async Task<IEnumerable<PaymentViewModel>> GetByInvoiceIdAsync(int invoiceId)
        {
            try
            {
                var payments = await _context.Payments
                    .Include(p => p.ProcessedByNavigation)
                    .Where(p => p.InvoiceId == invoiceId)
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new PaymentViewModel
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
                    })
                    .ToListAsync();

                return payments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payments by invoice id {InvoiceId}", invoiceId);
                throw;
            }
        }

        public async Task<IEnumerable<PaymentViewModel>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var start = DateOnly.FromDateTime(startDate);
                var end = DateOnly.FromDateTime(endDate);

                var payments = await _context.Payments
                    .Include(p => p.Invoice)
                    .Include(p => p.ProcessedByNavigation)
                    .Where(p => p.PaymentDate >= start && p.PaymentDate <= end)
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new PaymentViewModel
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
                    })
                    .ToListAsync();

                return payments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payments by date range {StartDate} - {EndDate}", startDate, endDate);
                throw;
            }
        }

        public async Task<decimal> GetTotalPaidAmountAsync(int invoiceId)
        {
            try
            {
                var totalPaid = await _context.Payments
                    .Where(p => p.InvoiceId == invoiceId)
                    .SumAsync(p => p.Amount);

                return totalPaid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total paid amount for invoice {InvoiceId}", invoiceId);
                throw;
            }
        }

        public async Task<string> GeneratePaymentNumberAsync()
        {
            try
            {
                var today = DateTime.Today;
                var prefix = $"PAY{today:yyyyMMdd}";
                var lastPayment = await _context.Payments
                    .Where(p => p.PaymentNumber.StartsWith(prefix))
                    .OrderByDescending(p => p.PaymentNumber)
                    .FirstOrDefaultAsync();

                if (lastPayment == null)
                {
                    return $"{prefix}001";
                }

                var lastNumber = int.Parse(lastPayment.PaymentNumber.Substring(prefix.Length));
                return $"{prefix}{(lastNumber + 1):D3}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating payment number");
                throw;
            }
        }

        public async Task<bool> ValidatePaymentAsync(CreatePaymentDto dto)
        {
            try
            {
                // Check if invoice exists
                var invoice = await _context.Invoices.FindAsync(dto.InvoiceId);
                if (invoice == null)
                {
                    return false;
                }

                // Check if invoice is not already fully paid
                if (invoice.Status == "paid")
                {
                    return false;
                }

                // Check if payment amount is positive
                if (dto.Amount <= 0)
                {
                    return false;
                }

                // Check if payment amount doesn't exceed remaining amount
                var totalPaid = await _context.Payments
                    .Where(p => p.InvoiceId == dto.InvoiceId)
                    .SumAsync(p => p.Amount);

                var remainingAmount = invoice.TotalAmount - totalPaid;
                if (dto.Amount > remainingAmount)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating payment");
                return false;
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
