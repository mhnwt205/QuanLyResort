using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyResort.Models;

namespace QuanLyResort.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CustomersController : Controller
    {
        private readonly ResortDbContext _context;

        public CustomersController(ResortDbContext context)
        {
            _context = context;
        }

        // GET: Customer/Customers/Index
        public IActionResult Index()
        {
            return View();
        }

        // GET: Customer/Customers/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: Customer/Customers/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind("CustomerId,FirstName,LastName,Email,Phone,Address,DateOfBirth,Gender,Nationality,IdNumber,PassportNumber")] Models.Customer customer)
        {
            if (ModelState.IsValid)
            {
                // Tạo mã khách hàng tự động
                customer.CustomerCode = await GenerateCustomerCode();
                customer.CreatedAt = DateTime.Now;
                customer.UpdatedAt = DateTime.Now;
                customer.CustomerType = "individual";

                _context.Add(customer);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đăng ký thành công! Bạn có thể đặt phòng ngay bây giờ.";
                return RedirectToAction("Index", "Home");
            }
            return View(customer);
        }

        // GET: Customer/Customers/Profile
        public async Task<IActionResult> Profile(int? id)
        {
            if (id == null)
            {
                // Trong thực tế, sẽ lấy từ session hoặc authentication
                return NotFound();
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            // Lấy lịch sử đặt phòng
            var bookings = await _context.Bookings
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .Where(b => b.CustomerId == id)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            // Lấy lịch sử đặt dịch vụ
            var serviceBookings = await _context.ServiceBookings
                .Include(sb => sb.Service)
                .Where(sb => sb.CustomerId == id)
                .OrderByDescending(sb => sb.CreatedAt)
                .ToListAsync();

            ViewBag.Bookings = bookings;
            ViewBag.ServiceBookings = serviceBookings;

            return View(customer);
        }

        // GET: Customer/Customers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        // POST: Customer/Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CustomerId,FirstName,LastName,Email,Phone,Address,DateOfBirth,Gender,Nationality,IdNumber,PassportNumber")] Models.Customer customer)
        {
            if (id != customer.CustomerId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    customer.UpdatedAt = DateTime.Now;
                    _context.Update(customer);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomerExists(customer.CustomerId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Profile), new { id = customer.CustomerId });
            }
            return View(customer);
        }

        // GET: Customer/Customers/MyBookings
        public async Task<IActionResult> MyBookings(int? customerId)
        {
            if (customerId == null)
            {
                // Trong thực tế, sẽ lấy từ session hoặc authentication
                return NotFound();
            }

            var bookings = await _context.Bookings
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .Where(b => b.CustomerId == customerId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        // GET: Customer/Customers/MyServices
        public async Task<IActionResult> MyServices(int? customerId)
        {
            if (customerId == null)
            {
                // Trong thực tế, sẽ lấy từ session hoặc authentication
                return NotFound();
            }

            var serviceBookings = await _context.ServiceBookings
                .Include(sb => sb.Service)
                .Where(sb => sb.CustomerId == customerId)
                .OrderByDescending(sb => sb.CreatedAt)
                .ToListAsync();

            return View(serviceBookings);
        }

        // POST: Customer/Customers/CancelBooking/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                booking.Status = "cancelled";
                booking.UpdatedAt = DateTime.Now;
                _context.Update(booking);

                // Cập nhật trạng thái phòng về Available
                var room = await _context.Rooms.FindAsync(booking.RoomId);
                if (room != null)
                {
                    room.Status = "available";
                    _context.Update(room);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Hủy đặt phòng thành công!";
            }
            return RedirectToAction(nameof(MyBookings), new { customerId = booking?.CustomerId });
        }

        // POST: Customer/Customers/CancelService/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelService(int id)
        {
            var serviceBooking = await _context.ServiceBookings.FindAsync(id);
            if (serviceBooking != null)
            {
                serviceBooking.Status = "cancelled";
                serviceBooking.UpdatedAt = DateTime.Now;
                _context.Update(serviceBooking);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Hủy đặt dịch vụ thành công!";
            }
            return RedirectToAction(nameof(MyServices), new { customerId = serviceBooking?.CustomerId });
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.CustomerId == id);
        }

        private async Task<string> GenerateCustomerCode()
        {
            var today = DateTime.Today;
            var prefix = $"CUS{today:yyyyMMdd}";
            var lastCustomer = await _context.Customers
                .Where(c => c.CustomerCode.StartsWith(prefix))
                .OrderByDescending(c => c.CustomerCode)
                .FirstOrDefaultAsync();

            if (lastCustomer == null)
            {
                return $"{prefix}001";
            }

            var lastNumber = int.Parse(lastCustomer.CustomerCode.Substring(prefix.Length));
            return $"{prefix}{(lastNumber + 1):D3}";
        }
    }
}
