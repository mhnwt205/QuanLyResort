namespace QuanLyResort.ViewModels
{
    public class EmployeeDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? Address { get; set; }
        public string Position { get; set; } = null!;
        public int? DepartmentId { get; set; }
        public DateOnly? HireDate { get; set; }
        public decimal? Salary { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateEmployeeDto
    {
        public string EmployeeCode { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? Address { get; set; }
        public string Position { get; set; } = null!;
        public int? DepartmentId { get; set; }
        public DateOnly? HireDate { get; set; }
        public decimal? Salary { get; set; }
        public string? Status { get; set; }
    }
}
