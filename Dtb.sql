-- =============================================
-- RESORT MANAGEMENT SYSTEM - SQL SERVER MIGRATION (FULL v1.0)
-- Chạy được trên SQL Server Management Studio (SSMS)
-- =============================================

-- 1. TẠO DATABASE (nếu chưa có)
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'ResortManagement')
BEGIN
    CREATE DATABASE ResortManagement;
END
GO

USE ResortManagement;
GO

-- =============================================
-- 2. BẢNG CẤU TRÚC
-- Tạo theo thứ tự để đảm bảo FK không lỗi
-- =============================================

-- Roles
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Roles]') AND type = N'U')
BEGIN
    CREATE TABLE Roles (
        RoleId INT IDENTITY(1,1) PRIMARY KEY,
        RoleName NVARCHAR(50) NOT NULL UNIQUE,
        Description NVARCHAR(255),
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2 DEFAULT GETDATE()
    );
END
GO

-- Departments
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Departments]') AND type = N'U')
BEGIN
    CREATE TABLE Departments (
        DepartmentId INT IDENTITY(1,1) PRIMARY KEY,
        DepartmentName NVARCHAR(100) NOT NULL,
        Description NVARCHAR(255),
        ManagerId INT NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2 DEFAULT GETDATE()
    );
END
GO

-- Employees
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Employees]') AND type = N'U')
BEGIN
    CREATE TABLE Employees (
        EmployeeId INT IDENTITY(1,1) PRIMARY KEY,
        EmployeeCode NVARCHAR(20) NOT NULL UNIQUE,
        FirstName NVARCHAR(50) NOT NULL,
        LastName NVARCHAR(50) NOT NULL,
        Email NVARCHAR(100) UNIQUE,
        Phone NVARCHAR(20),
        Address NVARCHAR(255),
        Position NVARCHAR(100),
        DepartmentId INT NULL,
        HireDate DATE,
        Salary DECIMAL(12,2),
        Status NVARCHAR(20) DEFAULT 'active',
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2 DEFAULT GETDATE(),
        FOREIGN KEY (DepartmentId) REFERENCES Departments(DepartmentId)
    );
END
GO

-- Users
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type = N'U')
BEGIN
    CREATE TABLE Users (
        UserId INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(50) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(255) NOT NULL,
        EmployeeId INT NULL,
        RoleId INT NULL,
        IsActive BIT DEFAULT 1,
        LastLogin DATETIME2,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2 DEFAULT GETDATE(),
        FOREIGN KEY (EmployeeId) REFERENCES Employees(EmployeeId),
        FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
    );
END
GO

-- Customers
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Customers]') AND type = N'U')
BEGIN
    CREATE TABLE Customers (
        CustomerId INT IDENTITY(1,1) PRIMARY KEY,
        CustomerCode NVARCHAR(20) NOT NULL UNIQUE,
        FirstName NVARCHAR(50) NOT NULL,
        LastName NVARCHAR(50) NOT NULL,
        Email NVARCHAR(100),
        Phone NVARCHAR(20),
        Address NVARCHAR(255),
        Nationality NVARCHAR(50),
        PassportNumber NVARCHAR(50),
        IdCardNumber NVARCHAR(50),
        DateOfBirth DATE,
        Gender NVARCHAR(10),
        CustomerType NVARCHAR(20) DEFAULT 'individual',
        LoyaltyPoints INT DEFAULT 0,
        Notes NVARCHAR(500),
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2 DEFAULT GETDATE()
    );
END
GO

-- RoomTypes
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RoomTypes]') AND type = N'U')
BEGIN
    CREATE TABLE RoomTypes (
        RoomTypeId INT IDENTITY(1,1) PRIMARY KEY,
        TypeName NVARCHAR(100) NOT NULL,
        Description NVARCHAR(255),
        BasePrice DECIMAL(12,2) NOT NULL,
        MaxOccupancy INT NOT NULL,
        Amenities NVARCHAR(500),
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2 DEFAULT GETDATE()
    );
END
GO

-- Rooms
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Rooms]') AND type = N'U')
BEGIN
    CREATE TABLE Rooms (
        RoomId INT IDENTITY(1,1) PRIMARY KEY,
        RoomNumber NVARCHAR(20) NOT NULL UNIQUE,
        RoomTypeId INT NULL,
        FloorNumber INT,
        Status NVARCHAR(20) DEFAULT 'available',
        LastCleaned DATETIME2,
        Notes NVARCHAR(255),
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2 DEFAULT GETDATE(),
        FOREIGN KEY (RoomTypeId) REFERENCES RoomTypes(RoomTypeId)
    );
END
GO

-- Bookings
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Bookings]') AND type = N'U')
BEGIN
    CREATE TABLE Bookings (
        BookingId INT IDENTITY(1,1) PRIMARY KEY,
        BookingCode NVARCHAR(20) NOT NULL UNIQUE,
        CustomerId INT NULL,
        RoomId INT NULL,
        CheckInDate DATE NOT NULL,
        CheckOutDate DATE NOT NULL,
        Adults INT DEFAULT 1,
        Children INT DEFAULT 0,
        Status NVARCHAR(20) DEFAULT 'pending',
        TotalAmount DECIMAL(12,2),
        DepositAmount DECIMAL(12,2) DEFAULT 0,
        SpecialRequests NVARCHAR(500),
        CreatedBy INT NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2 DEFAULT GETDATE(),
        FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId),
        FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId),
        FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
    );
END
GO

-- CheckIns
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CheckIns]') AND type = N'U')
BEGIN
    CREATE TABLE CheckIns (
        CheckInId INT IDENTITY(1,1) PRIMARY KEY,
        BookingId INT NULL,
        RoomId INT NULL,
        CheckInTime DATETIME2 DEFAULT GETDATE(),
        CheckOutTime DATETIME2,
        ActualAdults INT,
        ActualChildren INT,
        CheckedInBy INT NULL,
        CheckedOutBy INT NULL,
        Notes NVARCHAR(255),
        FOREIGN KEY (BookingId) REFERENCES Bookings(BookingId),
        FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId),
        FOREIGN KEY (CheckedInBy) REFERENCES Users(UserId),
        FOREIGN KEY (CheckedOutBy) REFERENCES Users(UserId)
    );
END
GO

-- ServiceCategories
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ServiceCategories]') AND type = N'U')
BEGIN
    CREATE TABLE ServiceCategories (
        CategoryId INT IDENTITY(1,1) PRIMARY KEY,
        CategoryName NVARCHAR(100) NOT NULL,
        Description NVARCHAR(255),
        CreatedAt DATETIME2 DEFAULT GETDATE()
    );
END
GO

-- Services
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Services]') AND type = N'U')
BEGIN
    CREATE TABLE Services (
        ServiceId INT IDENTITY(1,1) PRIMARY KEY,
        ServiceCode NVARCHAR(20) NOT NULL UNIQUE,
        ServiceName NVARCHAR(100) NOT NULL,
        CategoryId INT NULL,
        Description NVARCHAR(255),
        UnitPrice DECIMAL(12,2) NOT NULL,
        Unit NVARCHAR(20) DEFAULT 'item',
        IsActive BIT DEFAULT 1,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2 DEFAULT GETDATE(),
        FOREIGN KEY (CategoryId) REFERENCES ServiceCategories(CategoryId)
    );
END
GO

-- ServiceBookings
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ServiceBookings]') AND type = N'U')
BEGIN
    CREATE TABLE ServiceBookings (
        ServiceBookingId INT IDENTITY(1,1) PRIMARY KEY,
        BookingCode NVARCHAR(20) NOT NULL, -- liên kết qua BookingCode (theo thiết kế ban đầu)
        CustomerId INT NULL,
        ServiceId INT NULL,
        BookingDate DATE NOT NULL,
        ServiceDate DATE NOT NULL,
        Quantity INT DEFAULT 1,
        UnitPrice DECIMAL(12,2),
        TotalAmount DECIMAL(12,2),
        Status NVARCHAR(20) DEFAULT 'pending',
        SpecialRequests NVARCHAR(255),
        CreatedBy INT NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2 DEFAULT GETDATE(),
        FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId),
        FOREIGN KEY (ServiceId) REFERENCES Services(ServiceId),
        FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
    );
    CREATE UNIQUE INDEX UX_ServiceBookings_BookingCode_ServiceId ON ServiceBookings(BookingCode, ServiceId);
END
GO

-- Invoices
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Invoices]') AND type = N'U')
BEGIN
    CREATE TABLE Invoices (
        InvoiceId INT IDENTITY(1,1) PRIMARY KEY,
        InvoiceNumber NVARCHAR(20) NOT NULL UNIQUE,
        CustomerId INT NULL,
        BookingId INT NULL,
        InvoiceDate DATE NOT NULL,
        DueDate DATE,
        Subtotal DECIMAL(12,2) NOT NULL,
        TaxAmount DECIMAL(12,2) DEFAULT 0,
        DiscountAmount DECIMAL(12,2) DEFAULT 0,
        TotalAmount DECIMAL(12,2) NOT NULL,
        Status NVARCHAR(20) DEFAULT 'draft',
        PaymentMethod NVARCHAR(20),
        Notes NVARCHAR(255),
        CreatedBy INT NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2 DEFAULT GETDATE(),
        FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId),
        FOREIGN KEY (BookingId) REFERENCES Bookings(BookingId),
        FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
    );
END
GO

-- InvoiceItems
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InvoiceItems]') AND type = N'U')
BEGIN
    CREATE TABLE InvoiceItems (
        ItemId INT IDENTITY(1,1) PRIMARY KEY,
        InvoiceId INT NULL,
        ItemType NVARCHAR(20),
        ItemName NVARCHAR(200) NOT NULL,
        Description NVARCHAR(255),
        Quantity DECIMAL(10,2) DEFAULT 1,
        UnitPrice DECIMAL(12,2) NOT NULL,
        TotalPrice DECIMAL(12,2) NOT NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        FOREIGN KEY (InvoiceId) REFERENCES Invoices(InvoiceId)
    );
END
GO

-- Payments
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Payments]') AND type = N'U')
BEGIN
    CREATE TABLE Payments (
        PaymentId INT IDENTITY(1,1) PRIMARY KEY,
        PaymentNumber NVARCHAR(20) NOT NULL UNIQUE,
        InvoiceId INT NULL,
        PaymentDate DATE NOT NULL,
        Amount DECIMAL(12,2) NOT NULL,
        PaymentMethod NVARCHAR(20),
        ReferenceNumber NVARCHAR(100),
        Notes NVARCHAR(255),
        ProcessedBy INT NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        FOREIGN KEY (InvoiceId) REFERENCES Invoices(InvoiceId),
        FOREIGN KEY (ProcessedBy) REFERENCES Users(UserId)
    );
END
GO

-- Warehouses
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Warehouses]') AND type = N'U')
BEGIN
    CREATE TABLE Warehouses (
        WarehouseId INT IDENTITY(1,1) PRIMARY KEY,
        WarehouseName NVARCHAR(100) NOT NULL,
        Location NVARCHAR(200),
        ManagerId INT NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        FOREIGN KEY (ManagerId) REFERENCES Employees(EmployeeId)
    );
END
GO

-- Items
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Items]') AND type = N'U')
BEGIN
    CREATE TABLE Items (
        ItemId INT IDENTITY(1,1) PRIMARY KEY,
        ItemCode NVARCHAR(20) NOT NULL UNIQUE,
        ItemName NVARCHAR(100) NOT NULL,
        Description NVARCHAR(255),
        Unit NVARCHAR(20) NOT NULL,
        CostPrice DECIMAL(12,2),
        SellingPrice DECIMAL(12,2),
        MinStockLevel INT DEFAULT 0,
        IsActive BIT DEFAULT 1,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2 DEFAULT GETDATE()
    );
END
GO

-- Inventory
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Inventory]') AND type = N'U')
BEGIN
    CREATE TABLE Inventory (
        InventoryId INT IDENTITY(1,1) PRIMARY KEY,
        ItemId INT NULL,
        WarehouseId INT NULL,
        QuantityOnHand INT DEFAULT 0,
        QuantityReserved INT DEFAULT 0,
        LastUpdated DATETIME2 DEFAULT GETDATE(),
        FOREIGN KEY (ItemId) REFERENCES Items(ItemId),
        FOREIGN KEY (WarehouseId) REFERENCES Warehouses(WarehouseId),
        UNIQUE (ItemId, WarehouseId)
    );
END
GO

-- StockIssues
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[StockIssues]') AND type = N'U')
BEGIN
    CREATE TABLE StockIssues (
        IssueId INT IDENTITY(1,1) PRIMARY KEY,
        IssueNumber NVARCHAR(20) NOT NULL UNIQUE,
        WarehouseId INT NULL,
        DepartmentId INT NULL,
        IssueDate DATE NOT NULL,
        Purpose NVARCHAR(255),
        Status NVARCHAR(20) DEFAULT 'pending',
        RequestedBy INT NULL,
        ApprovedBy INT NULL,
        IssuedBy INT NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2 DEFAULT GETDATE(),
        FOREIGN KEY (WarehouseId) REFERENCES Warehouses(WarehouseId),
        FOREIGN KEY (DepartmentId) REFERENCES Departments(DepartmentId),
        FOREIGN KEY (RequestedBy) REFERENCES Users(UserId),
        FOREIGN KEY (ApprovedBy) REFERENCES Users(UserId),
        FOREIGN KEY (IssuedBy) REFERENCES Users(UserId)
    );
END
GO

-- AuditLogs
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AuditLogs]') AND type = N'U')
BEGIN
    CREATE TABLE AuditLogs (
        LogId INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NULL,
        Action NVARCHAR(100) NOT NULL,
        TableName NVARCHAR(50),
        RecordId INT,
        OldValues NVARCHAR(MAX),
        NewValues NVARCHAR(MAX),
        IpAddress NVARCHAR(45),
        UserAgent NVARCHAR(500),
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        FOREIGN KEY (UserId) REFERENCES Users(UserId)
    );
END
GO

-- =============================================
-- 3. DỮ LIỆU MẪU: Roles, Departments, RoomTypes, Rooms, ServiceCategories, Services, Customers
-- =============================================

-- Roles
IF NOT EXISTS (SELECT * FROM Roles WHERE RoleName = 'admin')
BEGIN
    INSERT INTO Roles (RoleName, Description)
    VALUES 
    ('admin', N'Quản trị viên hệ thống'),
    ('manager', N'Quản lý'),
    ('receptionist', N'Nhân viên lễ tân'),
    ('housekeeping', N'Nhân viên buồng phòng'),
    ('accountant', N'Kế toán'),
    ('cashier', N'Thu ngân'),
    ('sales', N'Nhân viên kinh doanh');
END
GO

-- Departments
IF NOT EXISTS (SELECT * FROM Departments WHERE DepartmentName = N'Quản lý')
BEGIN
    INSERT INTO Departments (DepartmentName, Description)
    VALUES
    (N'Quản lý', N'Ban quản lý resort'),
    (N'Lễ tân', N'Bộ phận lễ tân và đón tiếp khách'),
    (N'Buồng phòng', N'Bộ phận buồng phòng và dọn dẹp'),
    (N'Kế toán', N'Bộ phận kế toán và tài chính'),
    (N'Thu ngân', N'Bộ phận thu ngân'),
    (N'Kinh doanh', N'Bộ phận kinh doanh và bán hàng'),
    (N'Spa', N'Bộ phận spa và massage'),
    (N'Nhà hàng', N'Bộ phận nhà hàng và bar');
END
GO

-- RoomTypes
IF NOT EXISTS (SELECT * FROM RoomTypes WHERE TypeName = 'Standard Room')
BEGIN
    INSERT INTO RoomTypes (TypeName, Description, BasePrice, MaxOccupancy, Amenities) VALUES 
    ('Standard Room', N'Phòng tiêu chuẩn với view biển', 1500000, 2, N'WiFi, TV, Mini bar, Balcony'),
    ('Deluxe Room', N'Phòng deluxe với view biển và bồn tắm', 2500000, 2, N'WiFi, TV, Mini bar, Balcony, Bathtub, Sea view'),
    ('Suite', N'Suite cao cấp với phòng khách riêng', 4000000, 4, N'WiFi, TV, Mini bar, Balcony, Bathtub, Sea view, Living room, Kitchenette'),
    ('Presidential Suite', N'Suite tổng thống với đầy đủ tiện nghi', 8000000, 6, N'WiFi, TV, Mini bar, Balcony, Bathtub, Sea view, Living room, Kitchen, Butler service'),
    ('Villa', N'Villa riêng biệt với hồ bơi', 12000000, 8, N'WiFi, TV, Mini bar, Private pool, Garden, Kitchen, Butler service, Private beach access');
END
GO

-- Rooms
IF NOT EXISTS (SELECT * FROM Rooms WHERE RoomNumber = '101')
BEGIN
    INSERT INTO Rooms (RoomNumber, RoomTypeId, FloorNumber, Status) VALUES 
    ('101', 1, 1, 'available'), ('102', 1, 1, 'available'), ('103', 1, 1, 'occupied'), ('104', 1, 1, 'available'),
    ('201', 2, 2, 'available'), ('202', 2, 2, 'occupied'), ('203', 2, 2, 'available'), ('204', 2, 2, 'available'),
    ('301', 3, 3, 'available'), ('302', 3, 3, 'available'), ('303', 3, 3, 'available'), ('304', 3, 3, 'available'),
    ('401', 4, 4, 'available'), ('402', 4, 4, 'available'),
    ('V01', 5, 0, 'available'), ('V02', 5, 0, 'available');
END
GO

-- ServiceCategories
IF NOT EXISTS (SELECT * FROM ServiceCategories WHERE CategoryName = N'Spa & Wellness')
BEGIN
    INSERT INTO ServiceCategories (CategoryName, Description) VALUES 
    (N'Spa & Wellness', N'Dịch vụ spa và chăm sóc sức khỏe'),
    (N'Restaurant & Bar', N'Dịch vụ nhà hàng và bar'),
    (N'Recreation', N'Dịch vụ giải trí và thể thao'),
    (N'Transportation', N'Dịch vụ vận chuyển'),
    (N'Business Services', N'Dịch vụ kinh doanh');
END
GO

-- Services
IF NOT EXISTS (SELECT * FROM Services WHERE ServiceCode = 'SPA001')
BEGIN
    INSERT INTO Services (ServiceCode, ServiceName, CategoryId, Description, UnitPrice, Unit) VALUES 
    ('SPA001', N'Massage Thái', 1, N'Massage Thái truyền thống 60 phút', 800000, 'session'),
    ('SPA002', N'Massage Đá Nóng', 1, N'Massage với đá nóng 90 phút', 1200000, 'session'),
    ('RES001', N'Breakfast Buffet', 2, N'Buffet sáng cho 1 người', 200000, 'person'),
    ('RES002', N'Lunch Set Menu', 2, N'Set menu trưa cho 1 người', 350000, 'person'),
    ('REC001', N'Snorkeling Tour', 3, N'Tour lặn ống thở 3 giờ', 400000, 'person'),
    ('TRA001', N'Airport Transfer', 4, N'Đưa đón sân bay', 500000, 'trip');
END
GO

-- Customers
IF NOT EXISTS (SELECT * FROM Customers WHERE CustomerCode = 'CUST001')
BEGIN
    INSERT INTO Customers (CustomerCode, FirstName, LastName, Email, Phone, Nationality, CustomerType) VALUES 
    ('CUST001', N'John', N'Smith', 'john.smith@email.com', '+1234567890', N'American', 'individual'),
    ('CUST002', N'Maria', N'Garcia', 'maria.garcia@email.com', '+1234567891', N'Spanish', 'individual'),
    ('CUST003', N'Nguyễn', N'Văn Nam', 'nam.nguyen@email.com', '0901234567', N'Vietnamese', 'individual');
END
GO

-- =============================================
-- 4. TẠO NHÂN VIÊN VÀ NGƯỜI DÙNG MẪU (Admin, Reception, Accountant)
-- =============================================

-- Employees mẫu (Admin, Reception, Accountant)
IF NOT EXISTS (SELECT * FROM Employees WHERE EmployeeCode = 'EMP001')
BEGIN
    INSERT INTO Employees (EmployeeCode, FirstName, LastName, Email, Phone, Position, DepartmentId, HireDate, Salary, Status)
    VALUES
    ('EMP001', N'System', N'Administrator', 'admin@resort.com', '0000000000', N'Admin', 1, GETDATE(), 0, 'active'),
    ('EMP002', N'Lê', N'Thu Hằng', 'hang.le@resort.com', '0902345678', N'Lễ tân', 2, '2022-05-01', 8000000, 'active'),
    ('EMP003', N'Trần', N'Minh Quân', 'quan.tran@resort.com', '0903456789', N'Kế toán', 4, '2021-03-15', 10000000, 'active');
END
GO

-- Users mẫu (Username = Email để đăng nhập bằng email)
-- Mật khẩu sẽ được code C# tự động chuyển sang BCrypt khi login lần đầu
IF NOT EXISTS (SELECT * FROM Users WHERE Username = 'admin@resort.com')
BEGIN
    INSERT INTO Users (Username, PasswordHash, EmployeeId, RoleId, IsActive, CreatedAt, UpdatedAt)
    SELECT 
        'admin@resort.com',
        'Admin@123',
        (SELECT TOP 1 EmployeeId FROM Employees WHERE EmployeeCode = 'EMP001'),
        (SELECT TOP 1 RoleId FROM Roles WHERE RoleName = 'admin'),
        1, GETDATE(), GETDATE();
END
GO

IF NOT EXISTS (SELECT * FROM Users WHERE Username = 'hang.le@resort.com')
BEGIN
    INSERT INTO Users (Username, PasswordHash, EmployeeId, RoleId, IsActive, CreatedAt, UpdatedAt)
    SELECT 
        'hang.le@resort.com',
        'Reception@123',
        (SELECT TOP 1 EmployeeId FROM Employees WHERE EmployeeCode = 'EMP002'),
        (SELECT TOP 1 RoleId FROM Roles WHERE RoleName = 'receptionist'),
        1, GETDATE(), GETDATE();
END
GO

IF NOT EXISTS (SELECT * FROM Users WHERE Username = 'quan.tran@resort.com')
BEGIN
    INSERT INTO Users (Username, PasswordHash, EmployeeId, RoleId, IsActive, CreatedAt, UpdatedAt)
    SELECT 
        'quan.tran@resort.com',
        'Account@123',
        (SELECT TOP 1 EmployeeId FROM Employees WHERE EmployeeCode = 'EMP003'),
        (SELECT TOP 1 RoleId FROM Roles WHERE RoleName = 'accountant'),
        1, GETDATE(), GETDATE();
END
GO

-- =============================================
-- 5. DỮ LIỆU MẪU: Warehouses, Items, Inventory
-- =============================================

-- Warehouses
IF NOT EXISTS (SELECT * FROM Warehouses WHERE WarehouseName = N'Warehouse Central')
BEGIN
    INSERT INTO Warehouses (WarehouseName, Location, ManagerId) VALUES
    (N'Warehouse Central', N'Khu kho chính - Resort', (SELECT TOP 1 EmployeeId FROM Employees WHERE EmployeeCode = 'EMP001')),
    (N'Warehouse Spa', N'Kho Spa', (SELECT TOP 1 EmployeeId FROM Employees WHERE EmployeeCode = 'EMP002'));
END
GO

-- Items
IF NOT EXISTS (SELECT * FROM Items WHERE ItemCode = 'ITM001')
BEGIN
    INSERT INTO Items (ItemCode, ItemName, Description, Unit, CostPrice, SellingPrice, MinStockLevel) VALUES
    ('ITM001', N'Khăn tắm', N'Khăn tắm kích thước lớn', 'piece', 50000, 80000, 10),
    ('ITM002', N'Xà phòng', N'Xà phòng cao cấp', 'piece', 10000, 15000, 50),
    ('ITM003', N'Nước suối 500ml', N'Nước đóng chai', 'bottle', 5000, 10000, 200);
END
GO

-- Inventory
IF NOT EXISTS (SELECT * FROM Inventory WHERE ItemId = 1 AND WarehouseId = 1)
BEGIN
    INSERT INTO Inventory (ItemId, WarehouseId, QuantityOnHand, QuantityReserved)
    VALUES
    ((SELECT TOP 1 ItemId FROM Items WHERE ItemCode = 'ITM001'), (SELECT TOP 1 WarehouseId FROM Warehouses WHERE WarehouseName = N'Warehouse Central'), 100, 5),
    ((SELECT TOP 1 ItemId FROM Items WHERE ItemCode = 'ITM002'), (SELECT TOP 1 WarehouseId FROM Warehouses WHERE WarehouseName = N'Warehouse Central'), 500, 10),
    ((SELECT TOP 1 ItemId FROM Items WHERE ItemCode = 'ITM003'), (SELECT TOP 1 WarehouseId FROM Warehouses WHERE WarehouseName = N'Warehouse Central'), 1000, 50);
END
GO

-- =============================================
-- 6. DỮ LIỆU MẪU: Bookings, ServiceBookings, CheckIns, Invoices, InvoiceItems, Payments
-- =============================================

-- Bookings (3 mẫu)
IF NOT EXISTS (SELECT * FROM Bookings WHERE BookingCode = 'BKG001')
BEGIN
    INSERT INTO Bookings (BookingCode, CustomerId, RoomId, CheckInDate, CheckOutDate, Adults, Children, Status, TotalAmount, DepositAmount, SpecialRequests, CreatedBy)
    VALUES
    ('BKG001', (SELECT TOP 1 CustomerId FROM Customers WHERE CustomerCode = 'CUST001'), (SELECT TOP 1 RoomId FROM Rooms WHERE RoomNumber = '101'), CAST(GETDATE() AS DATE), DATEADD(DAY, 2, CAST(GETDATE() AS DATE)), 2, 0, 'confirmed', 3000000, 500000, N'Near sea view', (SELECT TOP 1 UserId FROM Users WHERE Username = 'hang.le@resort.com')),
    ('BKG002', (SELECT TOP 1 CustomerId FROM Customers WHERE CustomerCode = 'CUST002'), (SELECT TOP 1 RoomId FROM Rooms WHERE RoomNumber = '202'), DATEADD(DAY, 10, CAST(GETDATE() AS DATE)), DATEADD(DAY, 12, CAST(GETDATE() AS DATE)), 2, 1, 'pending', 5000000, 1000000, N'Extra bed', (SELECT TOP 1 UserId FROM Users WHERE Username = 'hang.le@resort.com')),
    ('BKG003', (SELECT TOP 1 CustomerId FROM Customers WHERE CustomerCode = 'CUST003'), (SELECT TOP 1 RoomId FROM Rooms WHERE RoomNumber = 'V01'), DATEADD(DAY, 20, CAST(GETDATE() AS DATE)), DATEADD(DAY, 23, CAST(GETDATE() AS DATE)), 4, 2, 'pending', 36000000, 5000000, N'Private pool access', (SELECT TOP 1 UserId FROM Users WHERE Username = 'hang.le@resort.com'));
END
GO

-- ServiceBookings (liên quan booking)
IF NOT EXISTS (SELECT * FROM ServiceBookings WHERE BookingCode = 'BKG001' AND ServiceId = (SELECT ServiceId FROM Services WHERE ServiceCode = 'SPA001'))
BEGIN
    INSERT INTO ServiceBookings (BookingCode, CustomerId, ServiceId, BookingDate, ServiceDate, Quantity, UnitPrice, TotalAmount, Status, SpecialRequests, CreatedBy)
    VALUES
    ('BKG001', (SELECT TOP 1 CustomerId FROM Customers WHERE CustomerCode = 'CUST001'), (SELECT TOP 1 ServiceId FROM Services WHERE ServiceCode = 'SPA001'), CAST(GETDATE() AS DATE), DATEADD(DAY, 0, CAST(GETDATE() AS DATE)), 1, 800000, 800000, 'confirmed', N'Prefer female therapist', (SELECT TOP 1 UserId FROM Users WHERE Username = 'hang.le@resort.com')),
    ('BKG002', (SELECT TOP 1 CustomerId FROM Customers WHERE CustomerCode = 'CUST002'), (SELECT TOP 1 ServiceId FROM Services WHERE ServiceCode = 'RES001'), DATEADD(DAY, 10, CAST(GETDATE() AS DATE)), DATEADD(DAY, 10, CAST(GETDATE() AS DATE)), 2, 200000, 400000, 'pending', N'Window seat', (SELECT TOP 1 UserId FROM Users WHERE Username = 'hang.le@resort.com'));
END
GO

-- CheckIns (sample)
IF NOT EXISTS (SELECT * FROM CheckIns WHERE BookingId = (SELECT BookingId FROM Bookings WHERE BookingCode = 'BKG001'))
BEGIN
    INSERT INTO CheckIns (BookingId, RoomId, CheckInTime, ActualAdults, ActualChildren, CheckedInBy, Notes)
    VALUES
    ((SELECT TOP 1 BookingId FROM Bookings WHERE BookingCode = 'BKG001'), (SELECT TOP 1 RoomId FROM Rooms WHERE RoomNumber = '101'), GETDATE(), 2, 0, (SELECT TOP 1 UserId FROM Users WHERE Username = 'hang.le@resort.com'), N'Checked in smoothly');
END
GO

-- Invoices (sample)
IF NOT EXISTS (SELECT * FROM Invoices WHERE InvoiceNumber = 'INV001')
BEGIN
    INSERT INTO Invoices (InvoiceNumber, CustomerId, BookingId, InvoiceDate, DueDate, Subtotal, TaxAmount, DiscountAmount, TotalAmount, Status, PaymentMethod, Notes, CreatedBy)
    VALUES
    ('INV001', (SELECT TOP 1 CustomerId FROM Customers WHERE CustomerCode = 'CUST001'), (SELECT TOP 1 BookingId FROM Bookings WHERE BookingCode = 'BKG001'), CAST(GETDATE() AS DATE), DATEADD(DAY, 7, CAST(GETDATE() AS DATE)), 3000000, 300000, 0, 3300000, 'unpaid', 'card', N'Booking BKG001', (SELECT TOP 1 UserId FROM Users WHERE Username = 'quan.tran@resort.com')),
    ('INV002', (SELECT TOP 1 CustomerId FROM Customers WHERE CustomerCode = 'CUST002'), (SELECT TOP 1 BookingId FROM Bookings WHERE BookingCode = 'BKG002'), DATEADD(DAY, 10, CAST(GETDATE() AS DATE)), DATEADD(DAY, 17, DATEADD(DAY, 10, CAST(GETDATE() AS DATE))), 5400000, 540000, 0, 5940000, 'draft', 'cash', N'Booking BKG002', (SELECT TOP 1 UserId FROM Users WHERE Username = 'quan.tran@resort.com'));
END
GO

-- InvoiceItems (sample)
IF NOT EXISTS (SELECT * FROM InvoiceItems WHERE InvoiceId = (SELECT InvoiceId FROM Invoices WHERE InvoiceNumber = 'INV001'))
BEGIN
    INSERT INTO InvoiceItems (InvoiceId, ItemType, ItemName, Description, Quantity, UnitPrice, TotalPrice)
    VALUES
    ((SELECT TOP 1 InvoiceId FROM Invoices WHERE InvoiceNumber = 'INV001'), 'room', N'Standard Room - 2 nights', N'Room 101 - Standard', 2, 1500000, 3000000),
    ((SELECT TOP 1 InvoiceId FROM Invoices WHERE InvoiceNumber = 'INV001'), 'service', N'Massage Thái', N'Massage 60 phút', 1, 800000, 800000);
END
GO

-- Payments (sample)
IF NOT EXISTS (SELECT * FROM Payments WHERE PaymentNumber = 'PAY001')
BEGIN
    INSERT INTO Payments (PaymentNumber, InvoiceId, PaymentDate, Amount, PaymentMethod, ReferenceNumber, Notes, ProcessedBy)
    VALUES
    ('PAY001', (SELECT TOP 1 InvoiceId FROM Invoices WHERE InvoiceNumber = 'INV001'), CAST(GETDATE() AS DATE), 500000, 'card', 'TRX12345', N'Partial deposit', (SELECT TOP 1 UserId FROM Users WHERE Username = 'cashier') );
    -- Note: 'cashier' user may not exist; if not, ProcessedBy will be NULL
END
GO

-- =============================================
-- 7. DỮ LIỆU MẪU: StockIssues (xuất kho mẫu)
-- =============================================
IF NOT EXISTS (SELECT * FROM StockIssues WHERE IssueNumber = 'ISSUE001')
BEGIN
    INSERT INTO StockIssues (IssueNumber, WarehouseId, DepartmentId, IssueDate, Purpose, Status, RequestedBy, ApprovedBy, IssuedBy)
    VALUES
    ('ISSUE001', (SELECT TOP 1 WarehouseId FROM Warehouses WHERE WarehouseName = N'Warehouse Central'), (SELECT TOP 1 DepartmentId FROM Departments WHERE DepartmentName = N'Buồng phòng'), CAST(GETDATE() AS DATE), N'Xuất khăn cho buồng phòng', 'approved', (SELECT TOP 1 UserId FROM Users WHERE Username = 'hang.le@resort.com'), (SELECT TOP 1 UserId FROM Users WHERE Username = 'admin@resort.com'), (SELECT TOP 1 UserId FROM Users WHERE Username = 'hang.le@resort.com'));
END
GO

-- =============================================
-- 8. TRIGGERS: tự động cập nhật UpdatedAt khi UPDATE
-- (tạo cho những bảng chính)
-- =============================================

CREATE OR ALTER TRIGGER trg_UpdateTimestamp_Roles
ON Roles
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Roles SET UpdatedAt = GETDATE() WHERE RoleId IN (SELECT DISTINCT RoleId FROM inserted);
END;
GO

CREATE OR ALTER TRIGGER trg_UpdateTimestamp_Departments
ON Departments
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Departments SET UpdatedAt = GETDATE() WHERE DepartmentId IN (SELECT DISTINCT DepartmentId FROM inserted);
END;
GO

CREATE OR ALTER TRIGGER trg_UpdateTimestamp_Employees
ON Employees
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Employees SET UpdatedAt = GETDATE() WHERE EmployeeId IN (SELECT DISTINCT EmployeeId FROM inserted);
END;
GO

CREATE OR ALTER TRIGGER trg_UpdateTimestamp_Users
ON Users
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Users SET UpdatedAt = GETDATE() WHERE UserId IN (SELECT DISTINCT UserId FROM inserted);
END;
GO

CREATE OR ALTER TRIGGER trg_UpdateTimestamp_Customers
ON Customers
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Customers SET UpdatedAt = GETDATE() WHERE CustomerId IN (SELECT DISTINCT CustomerId FROM inserted);
END;
GO

CREATE OR ALTER TRIGGER trg_UpdateTimestamp_Rooms
ON Rooms
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Rooms SET UpdatedAt = GETDATE() WHERE RoomId IN (SELECT DISTINCT RoomId FROM inserted);
END;
GO

CREATE OR ALTER TRIGGER trg_UpdateTimestamp_Services
ON Services
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Services SET UpdatedAt = GETDATE() WHERE ServiceId IN (SELECT DISTINCT ServiceId FROM inserted);
END;
GO

CREATE OR ALTER TRIGGER trg_UpdateTimestamp_Items
ON Items
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Items SET UpdatedAt = GETDATE() WHERE ItemId IN (SELECT DISTINCT ItemId FROM inserted);
END;
GO

-- =============================================
-- 9. CHÚ Ý / CLEANUP
-- =============================================
-- - Đăng nhập bằng EMAIL (Username = Email)
-- - Mật khẩu plain text sẽ tự động chuyển sang BCrypt khi login lần đầu
-- - Tài khoản mẫu:
--   + Admin: admin@resort.com / Admin@123
--   + Lễ tân: hang.le@resort.com / Reception@123
--   + Kế toán: quan.tran@resort.com / Account@123
-- =============================================

PRINT '✅ FULL ResortManagement schema + sample data created successfully!';
PRINT '';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '📋 TÀI KHOẢN MẪU (Đăng nhập bằng EMAIL):';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
PRINT '1️⃣  ADMIN:';
PRINT '   📧 Email: admin@resort.com';
PRINT '   🔒 Password: Admin@123';
PRINT '';
PRINT '2️⃣  LỄ TÂN:';
PRINT '   📧 Email: hang.le@resort.com';
PRINT '   🔒 Password: Reception@123';
PRINT '';
PRINT '3️⃣  KẾ TOÁN:';
PRINT '   📧 Email: quan.tran@resort.com';
PRINT '   🔒 Password: Account@123';
PRINT '━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━';
GO
