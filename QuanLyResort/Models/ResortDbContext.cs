using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace QuanLyResort.Models;

public partial class ResortDbContext : DbContext
{
    public ResortDbContext()
    {
    }

    public ResortDbContext(DbContextOptions<ResortDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<CheckIn> CheckIns { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Inventory> Inventories { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<InvoiceItem> InvoiceItems { get; set; }

    public virtual DbSet<Item> Items { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }
    
    public virtual DbSet<OnlinePayment> OnlinePayments { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<RoomType> RoomTypes { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<ServiceBooking> ServiceBookings { get; set; }

    public virtual DbSet<ServiceCategory> ServiceCategories { get; set; }

    public virtual DbSet<StockIssue> StockIssues { get; set; }

    public virtual DbSet<StockIssueItem> StockIssueItems { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Warehouse> Warehouses { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=(local);Initial Catalog=ResortManagement;Integrated Security=True;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__AuditLog__5E5486486FFCFADF");

            entity.Property(e => e.Action).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.TableName).HasMaxLength(50);
            entity.Property(e => e.UserAgent).HasMaxLength(500);

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__AuditLogs__UserI__7755B73D");
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PK__Bookings__73951AED29FD211B");

            entity.HasIndex(e => e.BookingCode, "UQ__Bookings__C6E56BD58DC7DD85").IsUnique();

            entity.Property(e => e.Adults).HasDefaultValue(1);
            entity.Property(e => e.BookingCode).HasMaxLength(20);
            entity.Property(e => e.Children).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DepositAmount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(12, 2)");
            entity.Property(e => e.SpecialRequests).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("pending");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK__Bookings__Create__44CA3770");

            entity.HasOne(d => d.Customer).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK__Bookings__Custom__42E1EEFE");

            entity.HasOne(d => d.Room).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("FK__Bookings__RoomId__43D61337");
        });

        modelBuilder.Entity<CheckIn>(entity =>
        {
            entity.HasKey(e => e.CheckInId).HasName("PK__CheckIns__E6497684F0404E75");

            entity.Property(e => e.CheckInTime).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Notes).HasMaxLength(255);

            entity.HasOne(d => d.Booking).WithMany(p => p.CheckIns)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK__CheckIns__Bookin__489AC854");

            entity.HasOne(d => d.CheckedInByNavigation).WithMany(p => p.CheckInCheckedInByNavigations)
                .HasForeignKey(d => d.CheckedInBy)
                .HasConstraintName("FK__CheckIns__Checke__4A8310C6");

            entity.HasOne(d => d.CheckedOutByNavigation).WithMany(p => p.CheckInCheckedOutByNavigations)
                .HasForeignKey(d => d.CheckedOutBy)
                .HasConstraintName("FK__CheckIns__Checke__4B7734FF");

            entity.HasOne(d => d.Room).WithMany(p => p.CheckIns)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("FK__CheckIns__RoomId__498EEC8D");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__Customer__A4AE64D821632DE0");

            entity.ToTable(tb => tb.HasTrigger("trg_UpdateTimestamp_Customers"));

            entity.HasIndex(e => e.CustomerCode, "UQ__Customer__066785217FDA83F1").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.CustomerCode).HasMaxLength(20);
            entity.Property(e => e.CustomerType)
                .HasMaxLength(20)
                .HasDefaultValue("individual");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.IdCardNumber).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.LoyaltyPoints).HasDefaultValue(0);
            entity.Property(e => e.Nationality).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.PassportNumber).HasMaxLength(50);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId).HasName("PK__Departme__B2079BED5CF81C59");

            entity.ToTable(tb => tb.HasTrigger("trg_UpdateTimestamp_Departments"));

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DepartmentName).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__Employee__7AD04F110E2B4F3D");

            entity.ToTable(tb => tb.HasTrigger("trg_UpdateTimestamp_Employees"));

            entity.HasIndex(e => e.EmployeeCode, "UQ__Employee__1F6425484686EF4A").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Employee__A9D10534B9637C32").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.EmployeeCode).HasMaxLength(20);
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Position).HasMaxLength(100);
            entity.Property(e => e.Salary).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("active");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Department).WithMany(p => p.Employees)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("FK__Employees__Depar__403A8C7D");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.InventoryId).HasName("PK__Inventor__F5FDE6B3BF049071");

            entity.ToTable("Inventory");

            entity.HasIndex(e => new { e.ItemId, e.WarehouseId }, "UQ__Inventor__F01E0975E478A213").IsUnique();

            entity.Property(e => e.LastUpdated).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.QuantityOnHand).HasDefaultValue(0);
            entity.Property(e => e.QuantityReserved).HasDefaultValue(0);

            entity.HasOne(d => d.Item).WithMany(p => p.Inventories)
                .HasForeignKey(d => d.ItemId)
                .HasConstraintName("FK__Inventory__ItemI__1F98B2C1");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.Inventories)
                .HasForeignKey(d => d.WarehouseId)
                .HasConstraintName("FK__Inventory__Wareh__208CD6FA");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId).HasName("PK__Invoices__D796AAB535E06C03");

            entity.HasIndex(e => e.InvoiceNumber, "UQ__Invoices__D776E981A72817F8").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DiscountAmount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(12, 2)");
            entity.Property(e => e.InvoiceNumber).HasMaxLength(20);
            entity.Property(e => e.Notes).HasMaxLength(255);
            entity.Property(e => e.PaymentMethod).HasMaxLength(20);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("draft");
            entity.Property(e => e.Subtotal).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.TaxAmount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(12, 2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Booking).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK__Invoices__Bookin__5D95E53A");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK__Invoices__Create__5E8A0973");

            entity.HasOne(d => d.Customer).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK__Invoices__Custom__5CA1C101");
        });

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.HasKey(e => e.ItemId).HasName("PK__InvoiceI__727E838BEC1BFA15");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.ItemName).HasMaxLength(200);
            entity.Property(e => e.ItemType).HasMaxLength(20);
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1m)
                .HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Invoice).WithMany(p => p.InvoiceItems)
                .HasForeignKey(d => d.InvoiceId)
                .HasConstraintName("FK__InvoiceIt__Invoi__634EBE90");
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.ItemId).HasName("PK__Items__727E838BBA65A186");

            entity.ToTable(tb => tb.HasTrigger("trg_UpdateTimestamp_Items"));

            entity.HasIndex(e => e.ItemCode, "UQ__Items__3ECC0FEABB6E0353").IsUnique();

            entity.Property(e => e.CostPrice).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ItemCode).HasMaxLength(20);
            entity.Property(e => e.ItemName).HasMaxLength(100);
            entity.Property(e => e.MinStockLevel).HasDefaultValue(0);
            entity.Property(e => e.SellingPrice).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Unit).HasMaxLength(20);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A3805F6CE53");

            entity.HasIndex(e => e.PaymentNumber, "UQ__Payments__E2C1723BCDD2EE96").IsUnique();

            entity.Property(e => e.Amount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Notes).HasMaxLength(255);
            entity.Property(e => e.PaymentMethod).HasMaxLength(20);
            entity.Property(e => e.PaymentNumber).HasMaxLength(20);
            entity.Property(e => e.ReferenceNumber).HasMaxLength(100);

            entity.HasOne(d => d.Invoice).WithMany(p => p.Payments)
                .HasForeignKey(d => d.InvoiceId)
                .HasConstraintName("FK__Payments__Invoic__681373AD");

            entity.HasOne(d => d.ProcessedByNavigation).WithMany(p => p.Payments)
                .HasForeignKey(d => d.ProcessedBy)
                .HasConstraintName("FK__Payments__Proces__690797E6");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1A432B6D94");

            entity.ToTable(tb => tb.HasTrigger("trg_UpdateTimestamp_Roles"));

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B61602F9F7A02").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.RoleName).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.RoomId).HasName("PK__Rooms__32863939D7229239");

            entity.ToTable(tb => tb.HasTrigger("trg_UpdateTimestamp_Rooms"));

            entity.HasIndex(e => e.RoomNumber, "UQ__Rooms__AE10E07A902E3E5F").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.RoomNumber).HasMaxLength(10);

            // Database Rooms table không có các cột này → bỏ map để tránh lỗi Invalid column name
            entity.Ignore(e => e.Description);
            entity.Ignore(e => e.Price);
            entity.Ignore(e => e.MaxOccupancy);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("available");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.RoomType).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.RoomTypeId)
                .HasConstraintName("FK__Rooms__RoomTypeI__59063A47");
        });

        modelBuilder.Entity<RoomType>(entity =>
        {
            entity.HasKey(e => e.RoomTypeId).HasName("PK__RoomType__BCC896318A554F10");

            entity.Property(e => e.Amenities).HasMaxLength(500);
            entity.Property(e => e.BasePrice).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.TypeName).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            // Một số DB không có các cột này → bỏ map
            entity.Ignore(e => e.Name);
            entity.Ignore(e => e.BaseRate);
            entity.Ignore(e => e.Capacity);
            // Nếu DB không có MaxOccupancy, bỏ comment dòng dưới
            // entity.Ignore(e => e.MaxOccupancy);
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PK__Services__C51BB00AD549391D");

            entity.ToTable(tb => tb.HasTrigger("trg_UpdateTimestamp_Services"));

            entity.HasIndex(e => e.ServiceCode, "UQ__Services__A01D74C9CC1C5CEE").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ServiceCode).HasMaxLength(20);
            entity.Property(e => e.ServiceName).HasMaxLength(100);
            entity.Property(e => e.ServiceCategory).HasMaxLength(20);
            entity.Property(e => e.Unit)
                .HasMaxLength(20)
                .HasDefaultValue("item");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Category).WithMany(p => p.Services)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__Services__Catego__72C60C4A");
        });

        modelBuilder.Entity<ServiceBooking>(entity =>
        {
            entity.HasKey(e => e.ServiceBookingId).HasName("PK__ServiceB__782948F478437FAA");

            entity.HasIndex(e => new { e.BookingCode, e.ServiceId }, "UX_ServiceBookings_BookingCode_ServiceId").IsUnique();

            entity.Property(e => e.BookingCode).HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Quantity).HasDefaultValue(1);
            entity.Property(e => e.SpecialRequests).HasMaxLength(255);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("pending");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ServiceBookings)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK__ServiceBo__Creat__540C7B00");

            entity.HasOne(d => d.Customer).WithMany(p => p.ServiceBookings)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK__ServiceBo__Custo__5224328E");

            entity.HasOne(d => d.Service).WithMany(p => p.ServiceBookings)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK__ServiceBo__Servi__531856C7");
        });

        modelBuilder.Entity<ServiceCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__ServiceC__19093A0B01E963C3");

            entity.Property(e => e.CategoryName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(255);
        });

        modelBuilder.Entity<StockIssue>(entity =>
        {
            entity.HasKey(e => e.IssueId).HasName("PK__StockIss__6C8616044D3039BB");

            entity.HasIndex(e => e.IssueNumber, "UQ__StockIss__5703F26C2754B64C").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IssueNumber).HasMaxLength(50);
            entity.Property(e => e.Purpose).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("pending");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.StockIssues)
                .HasForeignKey(d => d.WarehouseId)
                .HasConstraintName("FK__StockIssu__Wareh__6FB49575");

            entity.HasOne(d => d.Department).WithMany(p => p.StockIssues)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("FK__StockIssu__Depar__70A8B9AE");

            entity.HasOne(d => d.RequestedByNavigation).WithMany(p => p.StockIssueRequestedByNavigations)
                .HasForeignKey(d => d.RequestedBy)
                .HasConstraintName("FK__StockIssu__Reque__719CDDE7");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.StockIssueApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK__StockIssu__Appro__72910220");

            entity.HasOne(d => d.IssuedByNavigation).WithMany(p => p.StockIssueIssuedByNavigations)
                .HasForeignKey(d => d.IssuedBy)
                .HasConstraintName("FK__StockIssu__Issue__73852659");
        });

        modelBuilder.Entity<StockIssueItem>(entity =>
        {
            entity.HasKey(e => e.StockIssueItemId).HasName("PK__StockIss__6C8616044D3039BB");

            entity.Property(e => e.QuantityRequested).HasDefaultValue(1);
            entity.Property(e => e.QuantityIssued).HasDefaultValue(0);
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Notes).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.StockIssue).WithMany(p => p.StockIssueItems)
                .HasForeignKey(d => d.IssueId)
                .HasConstraintName("FK__StockIssueItems__StockIssue");

            entity.HasOne(d => d.Item).WithMany()
                .HasForeignKey(d => d.ItemId)
                .HasConstraintName("FK__StockIssueItems__Item");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C46599412");

            entity.ToTable(tb => tb.HasTrigger("trg_UpdateTimestamp_Users"));

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E415D35B17").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Username).HasMaxLength(50);

            entity.HasOne(d => d.Employee).WithMany(p => p.Users)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("FK__Users__EmployeeI__3864608B");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK__Users__RoleId__395884C4");
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(e => e.WarehouseId).HasName("PK__Warehous__2608AFF97E6F84B0");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.WarehouseName).HasMaxLength(100);

            entity.HasOne(d => d.Manager).WithMany(p => p.Warehouses)
                .HasForeignKey(d => d.ManagerId)
                .HasConstraintName("FK__Warehouse__Manag__123EB7A3");
        });

        modelBuilder.Entity<OnlinePayment>(entity =>
        {
            entity.HasKey(e => e.OnlinePaymentId).HasName("PK__OnlinePa__8F0B6E785B8A5D99");

            entity.Property(e => e.Amount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.PaymentMethod).HasMaxLength(20);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("pending");
            entity.Property(e => e.TransactionId).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Booking).WithMany(p => p.OnlinePayments)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK__OnlinePayments__BookingId");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
