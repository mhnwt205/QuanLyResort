using System;
using System.Collections.Generic;

namespace QuanLyResort.Models;

public partial class ServiceCategory
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();
}
