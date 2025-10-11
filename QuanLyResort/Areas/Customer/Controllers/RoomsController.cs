using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyResort.Models;
using QuanLyResort.ViewModels;

namespace QuanLyResort.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class RoomsController : Controller
    {
        private readonly ResortDbContext _context;

        public RoomsController(ResortDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? checkInDate, 
            string? checkOutDate, 
            int? guestCount, 
            int? roomTypeId, 
            decimal? maxPrice, 
            string? searchKeyword,
            bool? isSearch)
        {
            Console.WriteLine($"[RoomsController] Index params => checkIn:{checkInDate}, checkOut:{checkOutDate}, guests:{guestCount}, roomType:{roomTypeId}, maxPrice:{maxPrice}, keyword:{searchKeyword}, isSearch:{isSearch}");
            var searchModel = new SearchRoomViewModel();
            var rooms = new List<RoomSearchResultViewModel>();
            var hasSearchParams = (isSearch == true) && !(string.IsNullOrEmpty(checkInDate) 
                                    && string.IsNullOrEmpty(checkOutDate)
                                    && !guestCount.HasValue
                                    && !roomTypeId.HasValue
                                    && !maxPrice.HasValue
                                    && string.IsNullOrEmpty(searchKeyword));
            Console.WriteLine($"[RoomsController] hasSearchParams={hasSearchParams} raw: in='{checkInDate}' out='{checkOutDate}' guests={guestCount} type={roomTypeId} max={maxPrice} kw='{searchKeyword}'");

            // Nếu có tham số tìm kiếm, thực hiện tìm kiếm
            if (!string.IsNullOrEmpty(checkInDate) && !string.IsNullOrEmpty(checkOutDate))
            {
                var fmt = "yyyy-MM-dd";
                var ci = System.Globalization.CultureInfo.InvariantCulture;
                if (DateTime.TryParseExact(checkInDate, fmt, ci, System.Globalization.DateTimeStyles.None, out DateTime checkIn) &&
                    DateTime.TryParseExact(checkOutDate, fmt, ci, System.Globalization.DateTimeStyles.None, out DateTime checkOut))
                {
                    searchModel.CheckInDate = checkIn;
                    searchModel.CheckOutDate = checkOut;
                    searchModel.GuestCount = guestCount ?? 1;
                    searchModel.RoomTypeId = roomTypeId;
                    searchModel.MaxPrice = maxPrice;
                    searchModel.SearchKeyword = searchKeyword;

                    rooms = await SearchAvailableRoomsAsync(searchModel);
                    Console.WriteLine($"[RoomsController] after search => results:{rooms.Count}");
                }
            }
            else
            {
                // Hiển thị tất cả phòng nếu không có tìm kiếm
                rooms = await GetAllRoomsAsync();
                Console.WriteLine($"[RoomsController] no query params => all rooms:{rooms.Count}");
            }

            ViewBag.SearchModel = searchModel;
            ViewBag.HasSearch = hasSearchParams && searchModel.CheckInDate != default(DateTime) && searchModel.CheckOutDate != default(DateTime);
            Console.WriteLine($"[RoomsController] flags => hasSearchParams={hasSearchParams}, hasDates={(searchModel.CheckInDate != default(DateTime) && searchModel.CheckOutDate != default(DateTime))}, ViewBag.HasSearch={(bool)ViewBag.HasSearch}");
            ViewBag.RoomTypes = await _context.RoomTypes.ToListAsync();

            return View(rooms);
        }

        private async Task<List<RoomSearchResultViewModel>> SearchAvailableRoomsAsync(SearchRoomViewModel searchModel)
        {
            Console.WriteLine("[RoomsController] SearchAvailableRoomsAsync called");
            var query = _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Status == "available");

            // Lọc theo loại phòng
            if (searchModel.RoomTypeId.HasValue)
            {
                query = query.Where(r => r.RoomTypeId == searchModel.RoomTypeId.Value);
            }

            // Lọc theo giá tối đa (sử dụng BasePrice từ RoomType)
            if (searchModel.MaxPrice.HasValue)
            {
                query = query.Where(r => r.RoomType != null && r.RoomType.BasePrice <= searchModel.MaxPrice.Value);
            }

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(searchModel.SearchKeyword))
            {
                var keyword = searchModel.SearchKeyword.ToLower();
                query = query.Where(r => 
                    r.RoomNumber.ToLower().Contains(keyword) ||
                    (r.RoomType != null && r.RoomType.TypeName.ToLower().Contains(keyword)) ||
                    (r.RoomType != null && r.RoomType.Name.ToLower().Contains(keyword)));
            }

            // Kiểm tra phòng còn trống trong khoảng thời gian
            var availableRooms = new List<RoomSearchResultViewModel>();
            var allRooms = await query.ToListAsync();

            foreach (var room in allRooms)
            {
                var isAvailable = await IsRoomAvailableAsync(room.RoomId, searchModel.CheckInDate, searchModel.CheckOutDate);
                
                // Kiểm tra số khách có phù hợp không
                var maxOccupancy = room.RoomType?.MaxOccupancy ?? 2;
                if (maxOccupancy < searchModel.GuestCount)
                    continue;
                
                availableRooms.Add(new RoomSearchResultViewModel
                {
                    RoomId = room.RoomId,
                    RoomNumber = room.RoomNumber,
                    RoomTypeName = room.RoomType?.TypeName ?? "Không xác định",
                    Price = room.RoomType?.BasePrice ?? 0,
                    MaxOccupancy = maxOccupancy,
                    Description = room.RoomType?.Description ?? "",
                    Amenities = room.RoomType?.Amenities ?? "",
                    ImageUrl = GetRoomImageUrl(room.RoomTypeId),
                    IsAvailable = isAvailable,
                    Status = isAvailable ? "Còn trống" : "Đã đặt"
                });
            }

            return availableRooms.Where(r => r.IsAvailable).ToList();
        }

        private async Task<List<RoomSearchResultViewModel>> GetAllRoomsAsync()
        {
            var rooms = await _context.Rooms
                .Include(r => r.RoomType)
                .ToListAsync();

            return rooms.Select(room => new RoomSearchResultViewModel
            {
                RoomId = room.RoomId,
                RoomNumber = room.RoomNumber,
                RoomTypeName = room.RoomType?.TypeName ?? "Không xác định",
                Price = room.RoomType?.BasePrice ?? 0,
                MaxOccupancy = room.RoomType?.MaxOccupancy ?? 2,
                Description = room.RoomType?.Description ?? "",
                Amenities = room.RoomType?.Amenities ?? "",
                ImageUrl = GetRoomImageUrl(room.RoomTypeId),
                IsAvailable = room.Status == "available",
                Status = room.Status == "available" ? "Còn trống" : "Đã đặt"
            }).ToList();
        }

        private async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkInDate, DateTime checkOutDate)
        {
            var checkInDateOnly = DateOnly.FromDateTime(checkInDate);
            var checkOutDateOnly = DateOnly.FromDateTime(checkOutDate);
            
            var conflictingBookings = await _context.Bookings
                .Where(b => b.RoomId == roomId && 
                           b.Status != "cancelled" &&
                           ((b.CheckInDate < checkOutDateOnly && b.CheckOutDate > checkInDateOnly)))
                .AnyAsync();

            return !conflictingBookings;
        }

        private string GetRoomImageUrl(int roomTypeId)
        {
            // Map room type ID to image URL
            return roomTypeId switch
            {
                1 => "/deluxe-assets/images/room-1.jpg",
                2 => "/deluxe-assets/images/room-2.jpg",
                3 => "/deluxe-assets/images/room-3.jpg",
                4 => "/deluxe-assets/images/room-4.jpg",
                5 => "/deluxe-assets/images/room-5.jpg",
                6 => "/deluxe-assets/images/room-6.jpg",
                _ => "/deluxe-assets/images/room-1.jpg"
            };
        }

        public async Task<IActionResult> Details(int id)
        {
            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .Include(r => r.Bookings.Where(b => b.Status == "confirmed" || b.Status == "checked_in"))
                .FirstOrDefaultAsync(r => r.RoomId == id);

            if (room == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phòng.";
                return RedirectToAction("Index");
            }

            // Lấy các phòng cùng loại
            var similarRooms = await _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.RoomTypeId == room.RoomTypeId && r.RoomId != id)
                .Take(3)
                .ToListAsync();

            ViewBag.SimilarRooms = similarRooms;
            ViewBag.RoomTypes = await _context.RoomTypes.ToListAsync();

            return View(room);
        }
    }
}