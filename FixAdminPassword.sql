-- =============================================
-- SCRIPT CẬP NHẬT ADMIN - ĐĂNG NHẬP BẰNG EMAIL
-- Chạy script này trên SQL Server Management Studio (SSMS)
-- =============================================

USE ResortManagement;
GO

PRINT '🔍 Kiểm tra thông tin admin hiện tại...';
GO

SELECT 
    u.UserId,
    u.Username,
    LEFT(u.PasswordHash, 30) + '...' as PasswordHash_Preview,
    u.IsActive,
    r.RoleName,
    e.Email as EmployeeEmail
FROM Users u
LEFT JOIN Roles r ON u.RoleId = r.RoleId
LEFT JOIN Employees e ON u.EmployeeId = e.EmployeeId
WHERE u.Username = 'admin' OR u.Username = 'admin@resort.com';
GO

PRINT '';
PRINT '🔄 Đang cập nhật username admin thành email và reset mật khẩu...';
GO

-- CẬP NHẬT username thành email để đăng nhập bằng email
-- CẬP NHẬT mật khẩu về plain text, code C# sẽ tự động convert sang BCrypt khi login
UPDATE Users 
SET Username = 'admin@resort.com',
    PasswordHash = 'Admin@123',
    UpdatedAt = GETDATE()
WHERE Username = 'admin' OR Username = 'admin@resort.com';

PRINT '✅ Đã cập nhật thông tin admin!';
PRINT '';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '📋 THÔNG TIN ĐĂNG NHẬP ADMIN:';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '   📧 Email: admin@resort.com';
PRINT '   🔒 Password: Admin@123';
PRINT '   🌐 URL: /Account/Login';
PRINT '';
PRINT '⚠️  LƯU Ý:';
PRINT '   - Đăng nhập bằng EMAIL: admin@resort.com';
PRINT '   - Mật khẩu có chữ A viết HOA: Admin@123';
PRINT '   - Sau khi login lần đầu, mật khẩu sẽ tự động';
PRINT '     được chuyển sang BCrypt hash an toàn';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
GO

-- Kiểm tra kết quả sau khi update
PRINT '';
PRINT '🔍 Thông tin admin sau khi cập nhật:';
GO

SELECT 
    u.UserId,
    u.Username,
    LEFT(u.PasswordHash, 30) + '...' as PasswordHash_Preview,
    u.IsActive,
    r.RoleName,
    e.Email as EmployeeEmail,
    u.UpdatedAt
FROM Users u
LEFT JOIN Roles r ON u.RoleId = r.RoleId
LEFT JOIN Employees e ON u.EmployeeId = e.EmployeeId
WHERE u.Username = 'admin@resort.com';
GO

