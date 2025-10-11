using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyResort.Models;

public partial class Employee
{
    [Key]
    public int EmployeeId { get; set; }

    [Required]
    [StringLength(20)]
    public string EmployeeCode { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string FullName => $"{FirstName} {LastName}";

    [Required]
    [StringLength(100)]
    public string Email { get; set; } = null!;

    [Required]
    [StringLength(20)]
    public string Phone { get; set; } = null!;

    [StringLength(200)]
    public string? Address { get; set; }

    [Required]
    [StringLength(100)]
    public string Position { get; set; } = null!;

    public int? DepartmentId { get; set; }

    public DateOnly? HireDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Salary { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Active";

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public virtual Department? Department { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    public virtual ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();

    public virtual ICollection<StockIssue> StockIssueRequestedByNavigations { get; set; } = new List<StockIssue>();

    public virtual ICollection<StockIssue> StockIssueApprovedByNavigations { get; set; } = new List<StockIssue>();

    public virtual ICollection<StockIssue> StockIssueIssuedByNavigations { get; set; } = new List<StockIssue>();
}
