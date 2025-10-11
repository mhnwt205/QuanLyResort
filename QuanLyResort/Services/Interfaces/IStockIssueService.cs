using QuanLyResort.ViewModels;

namespace QuanLyResort.Services.Interfaces
{
    public interface IStockIssueService
    {
        Task<IEnumerable<StockIssueViewModel>> GetAllAsync();
        Task<StockIssueViewModel?> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateStockIssueDto dto, string createdByUser);
        Task<bool> ApproveAsync(int id, string approvedByUser);
        Task<bool> IssueAsync(int id, string issuedByUser);
        Task<bool> CancelAsync(int id, string cancelledByUser);
        Task<IEnumerable<StockIssueViewModel>> GetByDepartmentIdAsync(int departmentId);
        Task<IEnumerable<StockIssueViewModel>> GetByWarehouseIdAsync(int warehouseId);
        Task<IEnumerable<StockIssueViewModel>> GetByStatusAsync(string status);
        Task<bool> ValidateStockAvailabilityAsync(int itemId, int warehouseId, int quantity);
        Task<string> GenerateIssueNumberAsync();
    }
}
