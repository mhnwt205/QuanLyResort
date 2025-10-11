-- =============================================
-- SCRIPT KIỂM TRA VÀ SỬA ROLE ADMIN
-- Chạy script này trên SQL Server Management Studio (SSMS)
-- =============================================

USE ResortManagement;
GO

PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '🔍 BƯỚC 1: Kiểm tra tất cả Roles trong hệ thống';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
GO

SELECT 
    RoleId,
    RoleName,
    Description,
    DATALENGTH(RoleName) as RoleName_ByteLength,
    LEN(RoleName) as RoleName_Length
FROM Roles
ORDER BY RoleId;
GO

PRINT '';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '🔍 BƯỚC 2: Kiểm tra thông tin admin user';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
GO

SELECT 
    u.UserId,
    u.Username,
    LEFT(u.PasswordHash, 30) + '...' as PasswordHash_Preview,
    u.IsActive,
    u.RoleId as User_RoleId,
    r.RoleId as Role_RoleId,
    r.RoleName,
    e.Email as EmployeeEmail,
    e.Position
FROM Users u
LEFT JOIN Roles r ON u.RoleId = r.RoleId
LEFT JOIN Employees e ON u.EmployeeId = e.EmployeeId
WHERE u.Username = 'admin@resort.com' OR u.Username = 'admin';
GO

PRINT '';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '🔧 BƯỚC 3: Cập nhật Role name thành chữ hoa Admin';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
GO

-- Cập nhật tên role thành 'Admin' với chữ A viết hoa
UPDATE Roles
SET RoleName = 'Admin',
    UpdatedAt = GETDATE()
WHERE LOWER(RoleName) = 'admin';

-- Cập nhật các role khác cho đồng bộ
UPDATE Roles SET RoleName = 'Manager', UpdatedAt = GETDATE() WHERE LOWER(RoleName) = 'manager';
UPDATE Roles SET RoleName = 'Receptionist', UpdatedAt = GETDATE() WHERE LOWER(RoleName) = 'receptionist';
UPDATE Roles SET RoleName = 'Housekeeping', UpdatedAt = GETDATE() WHERE LOWER(RoleName) = 'housekeeping';
UPDATE Roles SET RoleName = 'Accountant', UpdatedAt = GETDATE() WHERE LOWER(RoleName) = 'accountant';
UPDATE Roles SET RoleName = 'Cashier', UpdatedAt = GETDATE() WHERE LOWER(RoleName) = 'cashier';
UPDATE Roles SET RoleName = 'Sales', UpdatedAt = GETDATE() WHERE LOWER(RoleName) = 'sales';
UPDATE Roles SET RoleName = 'Customer', UpdatedAt = GETDATE() WHERE LOWER(RoleName) = 'customer';

PRINT '✅ Đã cập nhật tên các roles!';
GO

PRINT '';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '🔧 BƯỚC 4: Đảm bảo admin user có RoleId đúng';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
GO

-- Đảm bảo admin user có RoleId chính xác
UPDATE Users
SET RoleId = (SELECT TOP 1 RoleId FROM Roles WHERE RoleName = 'Admin'),
    IsActive = 1,
    UpdatedAt = GETDATE()
WHERE Username = 'admin@resort.com';

PRINT '✅ Đã cập nhật RoleId cho admin user!';
GO

PRINT '';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '🔍 BƯỚC 5: Kiểm tra kết quả sau khi cập nhật';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
GO

SELECT 
    u.UserId,
    u.Username,
    LEFT(u.PasswordHash, 30) + '...' as PasswordHash_Preview,
    u.IsActive,
    u.RoleId as User_RoleId,
    r.RoleId as Role_RoleId,
    r.RoleName,
    e.Email as EmployeeEmail,
    e.Position,
    u.UpdatedAt
FROM Users u
LEFT JOIN Roles r ON u.RoleId = r.RoleId
LEFT JOIN Employees e ON u.EmployeeId = e.EmployeeId
WHERE u.Username = 'admin@resort.com';
GO

PRINT '';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '✅ HOÀN TẤT!';
PRINT '';
PRINT '📋 THÔNG TIN ĐĂNG NHẬP ADMIN:';
PRINT '   📧 Email: admin@resort.com';
PRINT '   🔒 Password: Admin@123';
PRINT '   🌐 URL: http://localhost:5124/Account/Login';
PRINT '';
PRINT '⚠️  LƯU Ý:';
PRINT '   - RoleName đã được cập nhật thành "Admin" (chữ A viết hoa)';
PRINT '   - Đăng xuất và đăng nhập lại để thấy thay đổi';
PRINT '   - Sau khi login, bạn sẽ được redirect đến /Admin/Dashboard';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
GO

