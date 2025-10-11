using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyResort.Models;
using QuanLyResort.ViewModels;

namespace QuanLyResort.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ResortDbContext _context;

        public HomeController(ResortDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Tạm thời không truy vấn database để tránh lỗi
            // Sẽ sử dụng dữ liệu mẫu trong view
            ViewBag.FeaturedRooms = new List<object>();
            ViewBag.Services = new List<object>();

            var model = new SearchRoomViewModel();
            return View(model);
        }

        [HttpPost]
        public IActionResult SearchRooms(SearchRoomViewModel model)
        {
            Console.WriteLine($"[HomeController] SearchRooms POST => in:{model.CheckInDate:yyyy-MM-dd}, out:{model.CheckOutDate:yyyy-MM-dd}, guests:{model.GuestCount}, type:{model.RoomTypeId}, max:{model.MaxPrice}, kw:{model.SearchKeyword}");
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            // Kiểm tra ngày check-out phải sau ngày check-in
            if (model.CheckOutDate <= model.CheckInDate)
            {
                ModelState.AddModelError("CheckOutDate", "Ngày trả phòng phải sau ngày nhận phòng");
                return View("Index", model);
            }

            // Chuẩn hóa format ngày về yyyy-MM-dd (tránh lỗi culture)
            var fmt = "yyyy-MM-dd";
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            var inStr = model.CheckInDate.ToString(fmt);
            var outStr = model.CheckOutDate.ToString(fmt);

            // Chuyển hướng đến trang Rooms với tham số tìm kiếm
            return RedirectToAction("Index", "Rooms", new { 
                area = "Customer",
                checkInDate = inStr,
                checkOutDate = outStr,
                guestCount = model.GuestCount,
                roomTypeId = model.RoomTypeId,
                maxPrice = model.MaxPrice,
                searchKeyword = model.SearchKeyword
            });
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

    }
}
