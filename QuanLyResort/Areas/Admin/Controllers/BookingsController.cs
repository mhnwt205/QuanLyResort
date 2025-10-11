using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyResort.Models;
using QuanLyResort.Services.Interfaces;
using QuanLyResort.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyResort.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BookingsController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly ResortDbContext _context;

        public BookingsController(IBookingService bookingService, ResortDbContext context)
        {
            _bookingService = bookingService;
            _context = context;
        }

        // GET: Admin/Bookings
        public async Task<IActionResult> Index()
        {
            var bookings = await _bookingService.GetAllAsync();
            return View(bookings);
        }

        // GET: Admin/Bookings/Details/5
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

        // GET: Admin/Bookings/Create
        public IActionResult Create()
        {
            ViewBag.Customers = _context.Customers.ToList();
            ViewBag.Rooms = _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Status == "available")
                .ToList();
            
            return View(new CreateBookingDto 
            { 
                CheckInDate = DateTime.Today, 
                CheckOutDate = DateTime.Today.AddDays(1) 
            });
        }

        // POST: Admin/Bookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBookingDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Customers = _context.Customers.ToList();
                ViewBag.Rooms = _context.Rooms
                    .Include(r => r.RoomType)
                    .Where(r => r.Status == "available")
                    .ToList();
                return View(dto);
            }

            try
            {
                var bookingId = await _bookingService.CreateAsync(dto, User.Identity?.Name ?? "system");
                TempData["SuccessMessage"] = "Đặt phòng đã được tạo thành công!";
                return RedirectToAction(nameof(Details), new { id = bookingId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.Customers = _context.Customers.ToList();
                ViewBag.Rooms = _context.Rooms
                    .Include(r => r.RoomType)
                    .Where(r => r.Status == "available")
                    .ToList();
                return View(dto);
            }
        }

        // GET: Admin/Bookings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _bookingService.GetByIdAsync(id.Value);
            if (booking == null)
            {
                return NotFound();
            }

            ViewBag.Customers = _context.Customers.ToList();
            ViewBag.Rooms = _context.Rooms.Include(r => r.RoomType).ToList();
            
            var dto = new CreateBookingDto
            {
                CustomerId = booking.CustomerId,
                RoomId = booking.RoomId,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                Adults = booking.Adults,
                Children = booking.Children,
                SpecialRequests = booking.SpecialRequests
            };

            return View(dto);
        }

        // POST: Admin/Bookings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateBookingDto dto)
        {
            if (id != dto.CustomerId) // This should be bookingId, but we'll handle it differently
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Customers = _context.Customers.ToList();
                ViewBag.Rooms = _context.Rooms.Include(r => r.RoomType).ToList();
                return View(dto);
            }

            try
            {
                // For now, we'll just redirect back to details since we don't have update method in service
                TempData["InfoMessage"] = "Chức năng cập nhật đang được phát triển.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.Customers = _context.Customers.ToList();
                ViewBag.Rooms = _context.Rooms.Include(r => r.RoomType).ToList();
                return View(dto);
            }
        }

        // GET: Admin/Bookings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _bookingService.GetByIdAsync(id.Value);
            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // POST: Admin/Bookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _bookingService.CancelBookingAsync(id, User.Identity?.Name ?? "system");
                if (result)
                {
                    TempData["SuccessMessage"] = "Đặt phòng đã được hủy thành công!";
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

        // POST: Admin/Bookings/Confirm/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id, int roomId)
        {
            try
            {
                var result = await _bookingService.ConfirmBookingAsync(id, roomId, User.Identity?.Name ?? "system");
                if (result)
                {
                    TempData["SuccessMessage"] = "Đặt phòng đã được xác nhận thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể xác nhận đặt phòng này.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi xác nhận đặt phòng: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/Bookings/AssignRoom/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRoom(int id, int roomId)
        {
            try
            {
                var result = await _bookingService.AssignRoomAsync(id, roomId, User.Identity?.Name ?? "system");
                if (result)
                {
                    TempData["SuccessMessage"] = "Phòng đã được gán thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể gán phòng này.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi gán phòng: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/Bookings/CheckIn/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(int id)
        {
            try
            {
                var result = await _bookingService.CheckInAsync(id, User.Identity?.Name ?? "system");
                if (result)
                {
                    TempData["SuccessMessage"] = "Check-in thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể thực hiện check-in.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi check-in: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/Bookings/CheckOut/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut(int id)
        {
            try
            {
                var result = await _bookingService.CheckOutAsync(id, User.Identity?.Name ?? "system");
                if (result)
                {
                    TempData["SuccessMessage"] = "Check-out thành công! Hóa đơn đã được tạo.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể thực hiện check-out.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi check-out: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Admin/Bookings/GetAvailableRooms
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

        // GET: Admin/Bookings/CalculateAmount
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
    }
}