using Microsoft.EntityFrameworkCore;
using QuanLyResort.Models;
using QuanLyResort.Services.Interfaces;
using QuanLyResort.ViewModels;

namespace QuanLyResort.Services
{
    public class StockIssueService : IStockIssueService
    {
        private readonly ResortDbContext _context;
        private readonly ILogger<StockIssueService> _logger;

        public StockIssueService(ResortDbContext context, ILogger<StockIssueService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<StockIssueViewModel>> GetAllAsync()
        {
            try
            {
                var stockIssues = await _context.StockIssues
                    .Include(si => si.Warehouse)
                    .Include(si => si.Department)
                    .Include(si => si.RequestedByNavigation)
                    .Include(si => si.IssuedByNavigation)
                    .Include(si => si.ApprovedByNavigation)
                    .OrderByDescending(si => si.CreatedAt)
                    .Select(si => new StockIssueViewModel
                    {
                        IssueId = si.IssueId,
                        IssueNumber = si.IssueNumber,
                        WarehouseId = si.WarehouseId ?? 0,
                        WarehouseName = si.Warehouse != null ? si.Warehouse.WarehouseName : "",
                        DepartmentId = si.DepartmentId ?? 0,
                        DepartmentName = si.Department != null ? si.Department.DepartmentName : "",
                        RequestedBy = si.RequestedBy,
                        RequestedByName = si.RequestedByNavigation != null ? si.RequestedByNavigation.FullName : "",
                        IssuedBy = si.IssuedBy,
                        IssuedByName = si.IssuedByNavigation != null ? si.IssuedByNavigation.FullName : "",
                        ApprovedBy = si.ApprovedBy,
                        ApprovedByName = si.ApprovedByNavigation != null ? si.ApprovedByNavigation.FullName : "",
                        Purpose = si.Purpose ?? "",
                        Status = si.Status ?? "",
                        CreatedAt = si.CreatedAt,
                        UpdatedAt = si.UpdatedAt
                    })
                    .ToListAsync();

                return stockIssues;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all stock issues");
                throw;
            }
        }

        public async Task<StockIssueViewModel?> GetByIdAsync(int id)
        {
            try
            {
                var stockIssue = await _context.StockIssues
                    .Include(si => si.Warehouse)
                    .Include(si => si.Department)
                    .Include(si => si.RequestedByNavigation)
                    .Include(si => si.IssuedByNavigation)
                    .Include(si => si.ApprovedByNavigation)
                    .FirstOrDefaultAsync(si => si.IssueId == id);

                if (stockIssue == null) return null;

                return new StockIssueViewModel
                {
                    IssueId = stockIssue.IssueId,
                    IssueNumber = stockIssue.IssueNumber,
                    WarehouseId = stockIssue.WarehouseId ?? 0,
                    WarehouseName = stockIssue.Warehouse != null ? stockIssue.Warehouse.WarehouseName : "",
                    DepartmentId = stockIssue.DepartmentId ?? 0,
                    DepartmentName = stockIssue.Department != null ? stockIssue.Department.DepartmentName : "",
                    RequestedBy = stockIssue.RequestedBy,
                    RequestedByName = stockIssue.RequestedByNavigation != null ? stockIssue.RequestedByNavigation.FullName : "",
                    IssuedBy = stockIssue.IssuedBy,
                    IssuedByName = stockIssue.IssuedByNavigation != null ? stockIssue.IssuedByNavigation.FullName : "",
                    ApprovedBy = stockIssue.ApprovedBy,
                    ApprovedByName = stockIssue.ApprovedByNavigation != null ? stockIssue.ApprovedByNavigation.FullName : "",
                    Purpose = stockIssue.Purpose ?? "",
                    Status = stockIssue.Status ?? "",
                    CreatedAt = stockIssue.CreatedAt,
                    UpdatedAt = stockIssue.UpdatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock issue by id {IssueId}", id);
                throw;
            }
        }

        public async Task<int> CreateAsync(CreateStockIssueDto dto, string createdByUser)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == createdByUser);
                var issueNumber = await GenerateIssueNumberAsync();

                // Validate stock availability for all items
                foreach (var item in dto.Items)
                {
                    if (!await ValidateStockAvailabilityAsync(item.ItemId, dto.WarehouseId, item.Quantity))
                    {
                        throw new InvalidOperationException($"Không đủ tồn kho cho vật tư ID {item.ItemId}");
                    }
                }

                var stockIssue = new StockIssue
                {
                    IssueNumber = issueNumber,
                    WarehouseId = dto.WarehouseId,
                    DepartmentId = dto.DepartmentId,
                    Purpose = dto.Purpose,
                    Status = "pending",
                    RequestedBy = user?.UserId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.StockIssues.Add(stockIssue);
                await _context.SaveChangesAsync();

                // Create stock issue items
                foreach (var item in dto.Items)
                {
                    var stockIssueItem = new StockIssueItem
                    {
                        IssueId = stockIssue.IssueId,
                        ItemId = item.ItemId,
                        QuantityRequested = item.Quantity,
                        QuantityIssued = 0, // Will be updated when issued
                        CreatedAt = DateTime.Now
                    };
                    _context.StockIssueItems.Add(stockIssueItem);
                }

                // Log audit
                await LogAuditAsync(user?.UserId, "CREATE", "StockIssues", stockIssue.IssueId, null, stockIssue);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Stock issue created successfully. IssueId: {IssueId}, Number: {IssueNumber}", 
                    stockIssue.IssueId, stockIssue.IssueNumber);

                return stockIssue.IssueId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating stock issue");
                throw;
            }
        }

        public async Task<bool> ApproveAsync(int id, string approvedByUser)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var stockIssue = await _context.StockIssues.FindAsync(id);
                if (stockIssue == null) return false;

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == approvedByUser);
                var oldValues = new { stockIssue.Status, stockIssue.ApprovedBy };

                stockIssue.Status = "approved";
                stockIssue.ApprovedBy = user?.UserId;
                stockIssue.UpdatedAt = DateTime.Now;

                _context.StockIssues.Update(stockIssue);

                // Log audit
                await LogAuditAsync(user?.UserId, "APPROVE", "StockIssues", id, oldValues, stockIssue);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Stock issue approved successfully. IssueId: {IssueId}", id);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error approving stock issue {IssueId}", id);
                return false;
            }
        }

        public async Task<bool> IssueAsync(int id, string issuedByUser)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var stockIssue = await _context.StockIssues
                    .Include(si => si.StockIssueItems)
                    .FirstOrDefaultAsync(si => si.IssueId == id);

                if (stockIssue == null) return false;

                if (stockIssue.Status != "approved")
                {
                    throw new InvalidOperationException("Stock issue must be approved before issuing");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == issuedByUser);
                var oldValues = new { stockIssue.Status, stockIssue.IssuedBy };

                // Issue each item
                foreach (var item in stockIssue.StockIssueItems)
                {
                    // Update inventory
                    var inventory = await _context.Inventories
                        .FirstOrDefaultAsync(i => i.ItemId == item.ItemId && i.WarehouseId == stockIssue.WarehouseId);

                    if (inventory == null)
                    {
                        throw new InvalidOperationException($"Inventory not found for item {item.ItemId} in warehouse {stockIssue.WarehouseId}");
                    }

                    if (inventory.QuantityOnHand < item.QuantityRequested)
                    {
                        throw new InvalidOperationException($"Insufficient stock for item {item.ItemId}");
                    }

                    inventory.QuantityOnHand -= item.QuantityRequested;
                        inventory.LastUpdated = DateTime.Now;
                    _context.Inventories.Update(inventory);

                    // Update stock issue item
                    item.QuantityIssued = item.QuantityRequested;
                    _context.StockIssueItems.Update(item);
                }

                stockIssue.Status = "issued";
                stockIssue.IssuedBy = user?.UserId;
                stockIssue.UpdatedAt = DateTime.Now;

                _context.StockIssues.Update(stockIssue);

                // Log audit
                await LogAuditAsync(user?.UserId, "ISSUE", "StockIssues", id, oldValues, stockIssue);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Stock issue completed successfully. IssueId: {IssueId}", id);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error issuing stock {IssueId}", id);
                return false;
            }
        }

        public async Task<bool> CancelAsync(int id, string cancelledByUser)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var stockIssue = await _context.StockIssues.FindAsync(id);
                if (stockIssue == null) return false;

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == cancelledByUser);
                var oldValues = new { stockIssue.Status };

                stockIssue.Status = "cancelled";
                stockIssue.UpdatedAt = DateTime.Now;

                _context.StockIssues.Update(stockIssue);

                // Log audit
                await LogAuditAsync(user?.UserId, "CANCEL", "StockIssues", id, oldValues, stockIssue);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Stock issue cancelled successfully. IssueId: {IssueId}", id);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error cancelling stock issue {IssueId}", id);
                return false;
            }
        }

        public async Task<IEnumerable<StockIssueViewModel>> GetByDepartmentIdAsync(int departmentId)
        {
            try
            {
                var stockIssues = await _context.StockIssues
                    .Include(si => si.Warehouse)
                    .Include(si => si.Department)
                    .Include(si => si.RequestedByNavigation)
                    .Where(si => si.DepartmentId == departmentId)
                    .OrderByDescending(si => si.CreatedAt)
                    .Select(si => new StockIssueViewModel
                    {
                        IssueId = si.IssueId,
                        IssueNumber = si.IssueNumber,
                        WarehouseId = si.WarehouseId ?? 0,
                        WarehouseName = si.Warehouse != null ? si.Warehouse.WarehouseName : "",
                        DepartmentId = si.DepartmentId ?? 0,
                        DepartmentName = si.Department != null ? si.Department.DepartmentName : "",
                        RequestedBy = si.RequestedBy,
                        RequestedByName = si.RequestedByNavigation != null ? si.RequestedByNavigation.FullName : "",
                        IssuedBy = si.IssuedBy,
                        IssuedByName = si.IssuedByNavigation != null ? si.IssuedByNavigation.FullName : "",
                        ApprovedBy = si.ApprovedBy,
                        ApprovedByName = si.ApprovedByNavigation != null ? si.ApprovedByNavigation.FullName : "",
                        Purpose = si.Purpose ?? "",
                        Status = si.Status ?? "",
                        CreatedAt = si.CreatedAt,
                        UpdatedAt = si.UpdatedAt
                    })
                    .ToListAsync();

                return stockIssues;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock issues by department id {DepartmentId}", departmentId);
                throw;
            }
        }

        public async Task<IEnumerable<StockIssueViewModel>> GetByWarehouseIdAsync(int warehouseId)
        {
            try
            {
                var stockIssues = await _context.StockIssues
                    .Include(si => si.Warehouse)
                    .Include(si => si.Department)
                    .Include(si => si.RequestedByNavigation)
                    .Where(si => si.WarehouseId == warehouseId)
                    .OrderByDescending(si => si.CreatedAt)
                    .Select(si => new StockIssueViewModel
                    {
                        IssueId = si.IssueId,
                        IssueNumber = si.IssueNumber,
                        WarehouseId = si.WarehouseId ?? 0,
                        WarehouseName = si.Warehouse != null ? si.Warehouse.WarehouseName : "",
                        DepartmentId = si.DepartmentId ?? 0,
                        DepartmentName = si.Department != null ? si.Department.DepartmentName : "",
                        RequestedBy = si.RequestedBy,
                        RequestedByName = si.RequestedByNavigation != null ? si.RequestedByNavigation.FullName : "",
                        IssuedBy = si.IssuedBy,
                        IssuedByName = si.IssuedByNavigation != null ? si.IssuedByNavigation.FullName : "",
                        ApprovedBy = si.ApprovedBy,
                        ApprovedByName = si.ApprovedByNavigation != null ? si.ApprovedByNavigation.FullName : "",
                        Purpose = si.Purpose ?? "",
                        Status = si.Status ?? "",
                        CreatedAt = si.CreatedAt,
                        UpdatedAt = si.UpdatedAt
                    })
                    .ToListAsync();

                return stockIssues;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock issues by warehouse id {WarehouseId}", warehouseId);
                throw;
            }
        }

        public async Task<IEnumerable<StockIssueViewModel>> GetByStatusAsync(string status)
        {
            try
            {
                var stockIssues = await _context.StockIssues
                    .Include(si => si.Warehouse)
                    .Include(si => si.Department)
                    .Include(si => si.RequestedByNavigation)
                    .Where(si => si.Status == status)
                    .OrderByDescending(si => si.CreatedAt)
                    .Select(si => new StockIssueViewModel
                    {
                        IssueId = si.IssueId,
                        IssueNumber = si.IssueNumber,
                        WarehouseId = si.WarehouseId ?? 0,
                        WarehouseName = si.Warehouse != null ? si.Warehouse.WarehouseName : "",
                        DepartmentId = si.DepartmentId ?? 0,
                        DepartmentName = si.Department != null ? si.Department.DepartmentName : "",
                        RequestedBy = si.RequestedBy,
                        RequestedByName = si.RequestedByNavigation != null ? si.RequestedByNavigation.FullName : "",
                        IssuedBy = si.IssuedBy,
                        IssuedByName = si.IssuedByNavigation != null ? si.IssuedByNavigation.FullName : "",
                        ApprovedBy = si.ApprovedBy,
                        ApprovedByName = si.ApprovedByNavigation != null ? si.ApprovedByNavigation.FullName : "",
                        Purpose = si.Purpose ?? "",
                        Status = si.Status ?? "",
                        CreatedAt = si.CreatedAt,
                        UpdatedAt = si.UpdatedAt
                    })
                    .ToListAsync();

                return stockIssues;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock issues by status {Status}", status);
                throw;
            }
        }

        public async Task<bool> ValidateStockAvailabilityAsync(int itemId, int warehouseId, int quantity)
        {
            try
            {
                var inventory = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ItemId == itemId && i.WarehouseId == warehouseId);

                if (inventory == null) return false;

                return inventory.QuantityOnHand >= quantity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating stock availability for item {ItemId} in warehouse {WarehouseId}", itemId, warehouseId);
                return false;
            }
        }

        public async Task<string> GenerateIssueNumberAsync()
        {
            try
            {
                var today = DateTime.Today;
                var prefix = $"STK{today:yyyyMMdd}";
                var lastIssue = await _context.StockIssues
                    .Where(si => si.IssueNumber.StartsWith(prefix))
                    .OrderByDescending(si => si.IssueNumber)
                    .FirstOrDefaultAsync();

                if (lastIssue == null)
                {
                    return $"{prefix}001";
                }

                var lastNumber = int.Parse(lastIssue.IssueNumber.Substring(prefix.Length));
                return $"{prefix}{(lastNumber + 1):D3}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating stock issue number");
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
