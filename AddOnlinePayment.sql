-- Script để tạo bảng OnlinePayments
-- Chạy script này trong SQL Server Management Studio hoặc Azure Data Studio

USE ResortManagement;
GO

-- Kiểm tra và tạo bảng OnlinePayments nếu chưa tồn tại
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OnlinePayments')
BEGIN
    CREATE TABLE [dbo].[OnlinePayments](
        [OnlinePaymentId] [int] IDENTITY(1,1) NOT NULL,
        [BookingId] [int] NOT NULL,
        [Amount] [decimal](12, 2) NOT NULL,
        [PaymentMethod] [nvarchar](20) NOT NULL,
        [Status] [nvarchar](20) NOT NULL DEFAULT ('pending'),
        [TransactionId] [nvarchar](100) NULL,
        [CreatedAt] [datetime] NOT NULL DEFAULT (getdate()),
        [UpdatedAt] [datetime] NULL,
        [CompletedAt] [datetime] NULL,
        CONSTRAINT [PK__OnlinePa__8F0B6E785B8A5D99] PRIMARY KEY CLUSTERED ([OnlinePaymentId] ASC),
        CONSTRAINT [FK__OnlinePayments__BookingId] FOREIGN KEY([BookingId])
            REFERENCES [dbo].[Bookings] ([BookingId])
    ) ON [PRIMARY];

    PRINT 'Bảng OnlinePayments đã được tạo thành công!';
END
ELSE
BEGIN
    PRINT 'Bảng OnlinePayments đã tồn tại.';
END
GO

-- Tạo index cho BookingId để tối ưu hóa truy vấn
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OnlinePayments_BookingId' AND object_id = OBJECT_ID('OnlinePayments'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_OnlinePayments_BookingId]
    ON [dbo].[OnlinePayments] ([BookingId] ASC);
    
    PRINT 'Index IX_OnlinePayments_BookingId đã được tạo!';
END
ELSE
BEGIN
    PRINT 'Index IX_OnlinePayments_BookingId đã tồn tại.';
END
GO

-- Kiểm tra dữ liệu
SELECT COUNT(*) AS TotalOnlinePayments FROM OnlinePayments;
GO

PRINT 'Hoàn tất! Bảng OnlinePayments đã sẵn sàng sử dụng.';

