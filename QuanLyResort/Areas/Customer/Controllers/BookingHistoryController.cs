using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyResort.Models;
using System.Security.Claims;

namespace QuanLyResort.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class BookingHistoryController : Controller
    {
        private readonly ResortDbContext _context;

        public BookingHistoryController(ResortDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Redirect("/Account/Login");
            if (!int.TryParse(userIdStr, out var userId)) return Redirect("/Account/Login");

            // Lấy thông tin user và customer
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return Redirect("/Account/Login");

            // Tìm customer theo email đăng nhập hoặc theo user ID nếu có liên kết
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == user.Username);
            
            // Nếu không tìm thấy, thử tìm theo các booking đã tạo bởi user này
            if (customer == null)
            {
                var userBookings = await _context.Bookings
                    .Where(b => b.CreatedBy == userId)
                    .Include(b => b.Customer)
                    .ToListAsync();
                
                if (userBookings.Any())
                {
                    // Lấy customer từ booking gần nhất
                    customer = userBookings.OrderByDescending(b => b.CreatedAt).First().Customer;
                }
            }

            if (customer == null)
            {
                TempData["ErrorMessage"] = $"Không tìm thấy thông tin khách hàng. Username: {user.Username}. Vui lòng liên hệ quản trị viên hoặc đăng ký lại tài khoản.";
                TempData["DebugInfo"] = $"UserId: {userId}, Username: {user.Username}";
                return Redirect("/Account/Profile");
            }

            // Lấy lịch sử đặt phòng - bao gồm cả booking của customer và booking được tạo bởi user
            var bookings = await _context.Bookings
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .Include(b => b.OnlinePayments)
                .Where(b => b.CustomerId == customer.CustomerId || b.CreatedBy == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            ViewBag.Customer = customer;
            ViewBag.TotalBookings = bookings.Count;
            return View(bookings);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Redirect("/Account/Login");
            if (!int.TryParse(userIdStr, out var userId)) return Redirect("/Account/Login");

            // Lấy thông tin user và customer
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return Redirect("/Account/Login");

            // Tìm customer theo email đăng nhập hoặc theo user ID nếu có liên kết
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == user.Username);
            
            // Nếu không tìm thấy, thử tìm theo các booking đã tạo bởi user này
            if (customer == null)
            {
                var userBookings = await _context.Bookings
                    .Where(b => b.CreatedBy == userId)
                    .Include(b => b.Customer)
                    .ToListAsync();
                
                if (userBookings.Any())
                {
                    // Lấy customer từ booking gần nhất
                    customer = userBookings.OrderByDescending(b => b.CreatedAt).First().Customer;
                }
            }

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng.";
                return Redirect("/Account/Profile");
            }

            // Lấy chi tiết đặt phòng - bao gồm cả booking của customer và booking được tạo bởi user
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .Include(b => b.OnlinePayments)
                .FirstOrDefaultAsync(b => b.BookingId == id && (b.CustomerId == customer.CustomerId || b.CreatedBy == userId));

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin đặt phòng.";
                return RedirectToAction("Index");
            }

            ViewBag.Customer = customer;
            return View(booking);
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Redirect("/Account/Login");
            if (!int.TryParse(userIdStr, out var userId)) return Redirect("/Account/Login");

            // Lấy thông tin user và customer
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return Redirect("/Account/Login");

            // Tìm customer theo email đăng nhập hoặc theo user ID nếu có liên kết
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == user.Username);
            
            // Nếu không tìm thấy, thử tìm theo các booking đã tạo bởi user này
            if (customer == null)
            {
                var userBookings = await _context.Bookings
                    .Where(b => b.CreatedBy == userId)
                    .Include(b => b.Customer)
                    .ToListAsync();
                
                if (userBookings.Any())
                {
                    // Lấy customer từ booking gần nhất
                    customer = userBookings.OrderByDescending(b => b.CreatedAt).First().Customer;
                }
            }

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng.";
                return Redirect("/Account/Profile");
            }

            // Lấy thông tin booking - bao gồm cả booking của customer và booking được tạo bởi user
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == id && (b.CustomerId == customer.CustomerId || b.CreatedBy == userId));

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin đặt phòng.";
                return RedirectToAction("Index");
            }

            // Kiểm tra điều kiện hủy (trước 24h)
            var checkInDateTime = booking.CheckInDate.ToDateTime(TimeOnly.MinValue);
            var timeUntilCheckIn = checkInDateTime - DateTime.Now;

            if (timeUntilCheckIn.TotalHours < 24)
            {
                TempData["ErrorMessage"] = "Không thể hủy đặt phòng. Chỉ có thể hủy trước 24 giờ so với thời gian nhận phòng.";
                return RedirectToAction("Details", new { id });
            }

            if (booking.Status == "cancelled")
            {
                TempData["ErrorMessage"] = "Đặt phòng này đã được hủy trước đó.";
                return RedirectToAction("Details", new { id });
            }

            try
            {
                // Cập nhật trạng thái booking
                booking.Status = "cancelled";
                booking.UpdatedAt = DateTime.Now;

                // Cập nhật trạng thái payment nếu có
                var payment = await _context.OnlinePayments
                    .FirstOrDefaultAsync(p => p.BookingId == booking.BookingId);

                if (payment != null)
                {
                    payment.Status = "cancelled";
                    payment.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Hủy đặt phòng thành công!";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra khi hủy đặt phòng: {ex.Message}";
                return RedirectToAction("Details", new { id });
            }
        }
    }
}
