using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyResort.Models;
using QuanLyResort.Services.Interfaces;
using QuanLyResort.ViewModels;

namespace QuanLyResort.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class BookingsController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly ResortDbContext _context;

        public BookingsController(IBookingService bookingService, ResortDbContext context)
        {
            _bookingService = bookingService;
            _context = context;
        }

        // GET: Customer/Bookings
        public async Task<IActionResult> Index(string? searchEmail, string? searchPhone)
        {
            IEnumerable<BookingViewModel> bookings;

            // Nếu user đã đăng nhập, lấy theo username/email của họ
            if (User.Identity?.IsAuthenticated == true)
            {
                var username = User.Identity.Name;
                var user = await _context.Users
                    .Include(u => u.Employee)
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (user != null)
                {
                    // Lấy bookings được tạo bởi user này HOẶC có email khớp với user
                    bookings = await _bookingService.GetAllAsync();
                    
                    // Tìm customer theo email của user (nếu có)
                    var userEmail = user.Employee?.Email ?? user.Username;
                    var customerIds = await _context.Customers
                        .Where(c => c.Email == userEmail)
                        .Select(c => c.CustomerId)
                        .ToListAsync();
                    
                    bookings = bookings.Where(b => 
                        b.CreatedBy == user.UserId.ToString() || 
                        customerIds.Contains(b.CustomerId)
                    ).ToList();
                }
                else
                {
                    bookings = new List<BookingViewModel>();
                }
            }
            // Nếu chưa đăng nhập, cho phép tra cứu bằng email hoặc phone
            else if (!string.IsNullOrEmpty(searchEmail) || !string.IsNullOrEmpty(searchPhone))
            {
                bookings = await _bookingService.GetAllAsync();
                
                if (!string.IsNullOrEmpty(searchEmail))
                {
                    bookings = bookings.Where(b => b.CustomerName.Contains(searchEmail, StringComparison.OrdinalIgnoreCase)).ToList();
                }
                
                if (!string.IsNullOrEmpty(searchPhone))
                {
                    // Tìm customer theo phone rồi lấy bookings
                    var customers = await _context.Customers
                        .Where(c => c.Phone != null && c.Phone.Contains(searchPhone))
                        .Select(c => c.CustomerId)
                        .ToListAsync();
                    
                    bookings = bookings.Where(b => customers.Contains(b.CustomerId)).ToList();
                }
            }
            else
            {
                // Chưa đăng nhập và chưa search → hiển thị form tra cứu
                bookings = new List<BookingViewModel>();
                ViewBag.ShowSearchForm = true;
            }

            // Sắp xếp theo ngày tạo mới nhất
            bookings = bookings.OrderByDescending(b => b.CreatedAt).ToList();
            
            return View(bookings);
        }

        // GET: Customer/Bookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _bookingService.GetDetailsByIdAsync(id.Value);
            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // GET: Customer/Bookings/Create
        public async Task<IActionResult> Create(int? roomId, DateTime? checkIn, DateTime? checkOut)
        {
            var rooms = await _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Status == "available")
                .ToListAsync();

            var customers = await _context.Customers.ToListAsync();

            ViewBag.Rooms = rooms;
            ViewBag.Customers = customers;
            ViewBag.SelectedRoomId = roomId;
            ViewBag.CheckInDate = checkIn ?? DateTime.Today.AddDays(1);
            ViewBag.CheckOutDate = checkOut ?? DateTime.Today.AddDays(2);

            return View(new CreateBookingDto 
            { 
                CheckInDate = checkIn ?? DateTime.Today.AddDays(1), 
                CheckOutDate = checkOut ?? DateTime.Today.AddDays(2),
                RoomId = roomId
            });
        }

        // POST: Customer/Bookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBookingDto dto)
        {
            if (!ModelState.IsValid)
            {
                var rooms = await _context.Rooms
                    .Include(r => r.RoomType)
                    .Where(r => r.Status == "available")
                    .ToListAsync();
                var customers = await _context.Customers.ToListAsync();

                ViewBag.Rooms = rooms;
                ViewBag.Customers = customers;
                return View(dto);
            }

            try
            {
                var bookingId = await _bookingService.CreateAsync(dto, "customer");
                TempData["SuccessMessage"] = "Đặt phòng thành công! Chúng tôi sẽ liên hệ với bạn để xác nhận.";
                return RedirectToAction("Details", "BookingHistory", new { id = bookingId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                var rooms = await _context.Rooms
                    .Include(r => r.RoomType)
                    .Where(r => r.Status == "available")
                    .ToListAsync();
                var customers = await _context.Customers.ToListAsync();

                ViewBag.Rooms = rooms;
                ViewBag.Customers = customers;
                return View(dto);
            }
        }

        // GET: Customer/Bookings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            var rooms = await _context.Rooms
                .Include(r => r.RoomType)
                .ToListAsync();
            var customers = await _context.Customers.ToListAsync();

            ViewBag.RoomId = rooms;
            ViewBag.CustomerId = customers;
            return View(booking);
        }

        // POST: Customer/Bookings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BookingId,CustomerId,RoomId,CheckInDate,CheckOutDate,TotalAmount,Status,SpecialRequests")] Booking booking)
        {
            if (id != booking.BookingId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(booking);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật đặt phòng thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Bookings.AnyAsync(e => e.BookingId == booking.BookingId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            var rooms = await _context.Rooms
                .Include(r => r.RoomType)
                .ToListAsync();
            var customers = await _context.Customers.ToListAsync();

            ViewBag.RoomId = rooms;
            ViewBag.CustomerId = customers;
            return View(booking);
        }

        // POST: Customer/Bookings/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var result = await _bookingService.CancelBookingAsync(id, "customer");
                if (result)
                {
                    TempData["SuccessMessage"] = "Hủy đặt phòng thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể hủy đặt phòng này.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi hủy đặt phòng: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Customer/Bookings/GetAvailableRooms
        [HttpGet]
        public async Task<IActionResult> GetAvailableRooms(DateTime checkIn, DateTime checkOut)
        {
            try
            {
                var rooms = await _context.Rooms
                    .Include(r => r.RoomType)
                    .Where(r => r.Status == "available")
                    .ToListAsync();

                var availableRooms = new List<object>();
                foreach (var room in rooms)
                {
                    var isAvailable = await _bookingService.IsRoomAvailableAsync(room.RoomId, checkIn, checkOut);
                    if (isAvailable)
                    {
                        availableRooms.Add(new
                        {
                            roomId = room.RoomId,
                            roomNumber = room.RoomNumber,
                            roomType = room.RoomType?.TypeName,
                            price = room.Price
                        });
                    }
                }

                return Json(availableRooms);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // GET: Customer/Bookings/CalculateAmount
        [HttpGet]
        public async Task<IActionResult> CalculateAmount(int roomId, DateTime checkIn, DateTime checkOut)
        {
            try
            {
                var amount = await _bookingService.CalculateTotalAmountAsync(roomId, checkIn, checkOut);
                return Json(new { amount = amount });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // GET: Customer/Bookings/All - Hiển thị tất cả bookings để test
        [HttpGet]
        public async Task<IActionResult> All()
        {
            var bookings = await _bookingService.GetAllAsync();
            bookings = bookings.OrderByDescending(b => b.CreatedAt).ToList();
            
            ViewBag.ShowSearchForm = false;
            return View("Index", bookings);
        }
    }
}
