using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyResort.Models;
using QuanLyResort.Services;
using QuanLyResort.Services.Interfaces;
using QuanLyResort.ViewModels;

namespace QuanLyResort.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class OnlineBookingController : Controller
    {
        private readonly ResortDbContext _context;
        private readonly IMoMoPaymentService _momoService;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;

        public OnlineBookingController(ResortDbContext context, IMoMoPaymentService momoService, IEmailSender emailSender, IConfiguration configuration)
        {
            _context = context;
            _momoService = momoService;
            _emailSender = emailSender;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Book(int roomId)
        {
            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.RoomId == roomId);

            if (room == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phòng.";
                return RedirectToAction("Index", "Rooms");
            }

            // Get room price
            decimal roomPrice = room.RoomType?.BasePrice ?? 1500000;
            if (roomPrice <= 0) roomPrice = 1500000;

            var model = new OnlineBookingViewModel
            {
                RoomId = room.RoomId,
                RoomName = room.RoomNumber ?? "Unknown",
                RoomType = room.RoomType?.TypeName ?? "Standard Room",
                RoomPrice = roomPrice,
                RoomImage = "/deluxe-assets/images/room-1.jpg",
                MaxOccupancy = room.RoomType?.MaxOccupancy ?? 2,
                CheckInDate = DateTime.Today.AddDays(1),
                CheckOutDate = DateTime.Today.AddDays(2),
                // Tự động điền email đăng nhập nếu user đã đăng nhập
                CustomerEmail = User.Identity?.IsAuthenticated == true ? User.Identity.Name : ""
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(OnlineBookingViewModel model)
        {
            Console.WriteLine($"=== BOOKING POST START ===");
            Console.WriteLine($"RoomId: {model.RoomId}");
            Console.WriteLine($"RoomName: {model.RoomName}");
            Console.WriteLine($"RoomType: {model.RoomType}");
            Console.WriteLine($"RoomPrice: {model.RoomPrice}");
            Console.WriteLine($"CheckInDate: {model.CheckInDate}");
            Console.WriteLine($"CheckOutDate: {model.CheckOutDate}");
            Console.WriteLine($"GuestCount: {model.GuestCount}");
            Console.WriteLine($"PaymentMethod: {model.PaymentMethod}");
            Console.WriteLine($"CustomerName: [{model.CustomerName}]");
            Console.WriteLine($"CustomerEmail: [{model.CustomerEmail}]");
            Console.WriteLine($"CustomerPhone: [{model.CustomerPhone}]");
            Console.WriteLine($"CustomerAddress: [{model.CustomerAddress}]");
            
            // Log all Form data để debug
            Console.WriteLine($"=== FORM DATA ===");
            foreach (var key in Request.Form.Keys)
            {
                Console.WriteLine($"{key} = [{Request.Form[key]}]");
            }

            // SAFEGUARD: Nếu RoomPrice = 0, reload từ database
            if (model.RoomPrice <= 0)
            {
                Console.WriteLine($"⚠️ WARNING: RoomPrice = {model.RoomPrice}, reloading from database...");
                var room = await _context.Rooms
                    .Include(r => r.RoomType)
                    .FirstOrDefaultAsync(r => r.RoomId == model.RoomId);
                
                if (room != null && room.RoomType != null)
                {
                    model.RoomPrice = room.RoomType.BasePrice;
                    Console.WriteLine($"✅ Reloaded RoomPrice: {model.RoomPrice}");
                }
            }
            
            // Remove calculated properties from ModelState validation
            ModelState.Remove("TotalAmount");
            ModelState.Remove("DepositAmount");
            ModelState.Remove("RemainingAmount");
            ModelState.Remove("TotalNights");
            ModelState.Remove("RoomName");
            ModelState.Remove("RoomType");
            ModelState.Remove("RoomImage");
            ModelState.Remove("MaxOccupancy");
            ModelState.Remove("IsValidDates");
            ModelState.Remove("RoomPrice"); // Lấy từ database, không từ form

            if (!ModelState.IsValid)
            {
                Console.WriteLine("❌ ModelState INVALID:");
                var errorMessages = new List<string>();
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key]?.Errors;
                    if (errors != null && errors.Count > 0)
                    {
                        foreach (var error in errors)
                        {
                            Console.WriteLine($"  - {key}: {error.ErrorMessage}");
                            errorMessages.Add($"{key}: {error.ErrorMessage}");
                        }
                    }
                }
                
                // ✅ Reload room info before returning view
                await ReloadRoomInfo(model);
                
                TempData["ErrorMessage"] = "Vui lòng kiểm tra lại thông tin đã nhập.";
                return View(model);
            }

            // Validate dates
            if (model.CheckOutDate <= model.CheckInDate)
            {
                ModelState.AddModelError("CheckOutDate", "Ngày trả phòng phải sau ngày nhận phòng.");
                await ReloadRoomInfo(model);
                TempData["ErrorMessage"] = "Ngày trả phòng phải sau ngày nhận phòng.";
                return View(model);
            }

            if (model.CheckInDate < DateTime.Today)
            {
                ModelState.AddModelError("CheckInDate", "Ngày nhận phòng phải từ hôm nay trở đi.");
                await ReloadRoomInfo(model);
                TempData["ErrorMessage"] = "Ngày nhận phòng phải từ hôm nay trở đi.";
                return View(model);
            }

            try
            {
                // Lấy thông tin phòng từ database để tính giá CHÍNH XÁC
                var room = await _context.Rooms
                    .Include(r => r.RoomType)
                    .FirstOrDefaultAsync(r => r.RoomId == model.RoomId);

                if (room == null || room.RoomType == null)
                {
                    ModelState.AddModelError("", "Không tìm thấy thông tin phòng.");
                    await ReloadRoomInfo(model);
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin phòng.";
                    return View(model);
                }

                // Tính TotalAmount từ database (KHÔNG TIN CLIENT)
                decimal roomPrice = room.RoomType.BasePrice;
                int totalNights = (model.CheckOutDate - model.CheckInDate).Days;
                decimal totalAmount = roomPrice * totalNights;

                Console.WriteLine($"=== CALCULATED VALUES ===");
                Console.WriteLine($"RoomPrice from DB: {roomPrice}");
                Console.WriteLine($"TotalNights: {totalNights}");
                Console.WriteLine($"TotalAmount: {totalAmount}");
                Console.WriteLine($"=========================");

                // Tạo booking code
                var bookingCode = $"BK{DateTime.Now:yyyyMMddHHmmss}{model.RoomId}";

                // Ưu tiên email đăng nhập nếu user đã đăng nhập
                string customerEmail = User.Identity?.IsAuthenticated == true ? User.Identity.Name : model.CustomerEmail;
                
                // Tạo hoặc tìm customer
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Email == customerEmail);

                if (customer == null)
                {
                    customer = new Models.Customer
                    {
                        CustomerCode = $"CUS{DateTime.Now:yyyyMMddHHmmss}",
                        FirstName = model.CustomerName.Split(' ').Last(),
                        LastName = string.Join(" ", model.CustomerName.Split(' ').SkipLast(1)),
                        Email = customerEmail, // Sử dụng email đã được xử lý
                        Phone = model.CustomerPhone,
                        Address = model.CustomerAddress,
                        CreatedAt = DateTime.Now
                    };
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Created new customer: {customer.CustomerCode}");
                }

                // Tạo booking
                var booking = new Booking
                {
                    BookingCode = bookingCode,
                    CustomerId = customer.CustomerId,
                    RoomId = model.RoomId,
                    CheckInDate = DateOnly.FromDateTime(model.CheckInDate),
                    CheckOutDate = DateOnly.FromDateTime(model.CheckOutDate),
                    Adults = model.GuestCount,
                    TotalAmount = totalAmount,  // ← Dùng giá tính từ SERVER
                    Status = "pending_payment",
                    SpecialRequests = model.SpecialRequests,
                    CreatedBy = User.Identity?.IsAuthenticated == true ? 
                        await GetUserIdAsync(User.Identity.Name) : null,
                    CreatedAt = DateTime.Now
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Created booking: {booking.BookingCode}, TotalAmount: {booking.TotalAmount}");

                // Xử lý thanh toán
                if (model.PaymentMethod == "momo")
                {
                    Console.WriteLine("→ Processing MoMo payment...");
                    return await ProcessMoMoPayment(booking, model, roomPrice, totalNights);
                }
                else if (model.PaymentMethod == "cash")
                {
                    Console.WriteLine("→ Processing Cash payment...");
                    return await ProcessCashPayment(booking, model, totalAmount);
                }

                Console.WriteLine("❌ No payment method selected!");
                TempData["ErrorMessage"] = "Vui lòng chọn phương thức thanh toán.";
                return RedirectToAction("PaymentResult", new { bookingId = booking.BookingId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                ModelState.AddModelError("", $"Có lỗi xảy ra: {ex.Message}");
                
                // ✅ Reload room info before returning view
                await ReloadRoomInfo(model);
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                return View(model);
            }
        }

        private async Task<IActionResult> ProcessCashPayment(Booking booking, OnlineBookingViewModel model, decimal totalAmount)
        {
            try
            {
                decimal depositAmount = Math.Round(totalAmount * 0.3m);

                // Tạo payment record
                var payment = new OnlinePayment
                {
                    BookingId = booking.BookingId,
                    Amount = depositAmount,
                    PaymentMethod = "cash",
                    Status = "pending_deposit",
                    CreatedAt = DateTime.Now
                };
                _context.OnlinePayments.Add(payment);
                
                // Update booking status
                booking.Status = "pending_deposit";
                booking.DepositAmount = depositAmount;
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Cash payment created: Deposit = {depositAmount:N0} VNĐ");

                // Gửi email
                await SendDepositEmail(booking, model, totalAmount, depositAmount);

                TempData["SuccessMessage"] = "Đặt phòng thành công! Vui lòng kiểm tra email để biết hướng dẫn thanh toán cọc.";
                return RedirectToAction("PaymentResult", new { bookingId = booking.BookingId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ProcessCashPayment Error: {ex.Message}");
                TempData["ErrorMessage"] = $"Lỗi xử lý thanh toán: {ex.Message}";
                return RedirectToAction("PaymentResult", new { bookingId = booking.BookingId });
            }
        }

        private async Task<IActionResult> ProcessMoMoPayment(Booking booking, OnlineBookingViewModel model, decimal roomPrice, int totalNights)
        {
            try
            {
                decimal totalAmount = roomPrice * totalNights;

                // Lấy cấu hình MoMo từ appsettings.json
                var partnerCode = _configuration["MoMo:PartnerCode"] ?? "MOMO";
                var accessKey = _configuration["MoMo:AccessKey"] ?? "F8BBA842ECF85";
                var secretKey = _configuration["MoMo:SecretKey"] ?? "K951B6PE1waDMi640xX08PD3vg6EkVlz";

                var momoRequest = new MoMoPaymentRequest
                {
                    PartnerCode = partnerCode,
                    AccessKey = accessKey,
                    SecretKey = secretKey,
                    RequestId = Guid.NewGuid().ToString(),
                    Amount = (long)totalAmount,
                    OrderId = booking.BookingCode,
                    OrderInfo = $"Thanh toán đặt phòng {model.RoomName} - {booking.BookingCode}",
                    ReturnUrl = $"{Request.Scheme}://{Request.Host}{Url.Action("MoMoReturn", "OnlineBooking", new { bookingId = booking.BookingId })}",
                    NotifyUrl = $"{Request.Scheme}://{Request.Host}{Url.Action("MoMoCallback", "OnlineBooking")}",
                    RequestType = "captureWallet",
                    ExtraData = ""
                };

                Console.WriteLine($"→ Creating MoMo payment: Amount = {totalAmount:N0} VNĐ");
                Console.WriteLine($"→ Using MoMo config: PartnerCode={partnerCode}, AccessKey={accessKey}");
                Console.WriteLine($"→ MoMo Request details:");
                Console.WriteLine($"  - OrderId: {momoRequest.OrderId}");
                Console.WriteLine($"  - OrderInfo: {momoRequest.OrderInfo}");
                Console.WriteLine($"  - ReturnUrl: {momoRequest.ReturnUrl}");
                Console.WriteLine($"  - NotifyUrl: {momoRequest.NotifyUrl}");

                // Generate signature - sử dụng thứ tự đúng theo MoMo API
                var rawHash = $"accessKey={momoRequest.AccessKey}&amount={momoRequest.Amount}&extraData={momoRequest.ExtraData}&ipnUrl={momoRequest.NotifyUrl}&orderId={momoRequest.OrderId}&orderInfo={momoRequest.OrderInfo}&partnerCode={momoRequest.PartnerCode}&redirectUrl={momoRequest.ReturnUrl}&requestId={momoRequest.RequestId}&requestType={momoRequest.RequestType}";
                momoRequest.Signature = _momoService.GenerateSignature(rawHash);
                
                Console.WriteLine($"→ RawHash: {rawHash}");
                Console.WriteLine($"→ Signature: {momoRequest.Signature}");

                var momoResponse = await _momoService.CreatePaymentAsync(momoRequest);
                Console.WriteLine($"MoMo Response: ResultCode={momoResponse.ResultCode}, Message={momoResponse.Message}");

                if (momoResponse.ResultCode == 0)
                {
                    // Lưu payment record
                    var payment = new OnlinePayment
                    {
                        BookingId = booking.BookingId,
                        Amount = totalAmount,
                        PaymentMethod = "momo",
                        Status = "pending",
                        TransactionId = momoRequest.RequestId,
                        CreatedAt = DateTime.Now
                    };
                    _context.OnlinePayments.Add(payment);
                    await _context.SaveChangesAsync();

                    Console.WriteLine($"✅ Redirecting to MoMo: {momoResponse.PayUrl}");
                    return Redirect(momoResponse.PayUrl);
                }
                else
                {
                    TempData["ErrorMessage"] = $"Lỗi thanh toán MoMo: {momoResponse.Message}";
                    return RedirectToAction("PaymentResult", new { bookingId = booking.BookingId });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ProcessMoMoPayment Error: {ex.Message}");
                TempData["ErrorMessage"] = $"Lỗi xử lý thanh toán MoMo: {ex.Message}";
                return RedirectToAction("PaymentResult", new { bookingId = booking.BookingId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> MoMoReturn(int bookingId, int resultCode)
        {
            Console.WriteLine($"=== MOMO RETURN === BookingId: {bookingId}, ResultCode: {resultCode}");

            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin đặt phòng.";
                return RedirectToAction("Index", "Rooms");
            }

            var payment = await _context.OnlinePayments
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync(p => p.BookingId == bookingId);

            if (resultCode == 0 && payment != null)
            {
                // Thanh toán thành công
                payment.Status = "completed";
                payment.CompletedAt = DateTime.Now;
                payment.UpdatedAt = DateTime.Now;

                booking.Status = "confirmed";
                booking.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                await AddLoyaltyPoints(booking.CustomerId, 5);
                await SendConfirmationEmail(booking);

                TempData["SuccessMessage"] = "Thanh toán MoMo thành công! Đặt phòng đã được xác nhận.";
                Console.WriteLine($"✅ MoMo payment completed for {booking.BookingCode}");
            }
            else
            {
                // Thanh toán thất bại
                if (payment != null)
                {
                    payment.Status = "failed";
                    payment.UpdatedAt = DateTime.Now;
                }
                booking.Status = "cancelled";
                await _context.SaveChangesAsync();

                TempData["ErrorMessage"] = "Thanh toán MoMo thất bại. Đặt phòng đã bị hủy.";
                Console.WriteLine($"❌ MoMo payment failed for {booking.BookingCode}");
            }

            return RedirectToAction("PaymentResult", new { bookingId = booking.BookingId });
        }

        [HttpPost]
        public async Task<IActionResult> MoMoCallback([FromBody] MoMoCallbackData callbackData)
        {
            try
            {
                if (!_momoService.VerifySignature(callbackData))
                {
                    return BadRequest("Invalid signature");
                }

                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.BookingCode == callbackData.OrderId);

                if (booking == null) return BadRequest("Booking not found");

                var payment = await _context.OnlinePayments
                    .FirstOrDefaultAsync(p => p.BookingId == booking.BookingId);

                if (payment == null) return BadRequest("Payment not found");

                if (callbackData.ResultCode == 0)
                {
                    payment.Status = "completed";
                    payment.TransactionId = callbackData.TransId.ToString();
                    payment.CompletedAt = DateTime.Now;

                    booking.Status = "confirmed";
                    await AddLoyaltyPoints(booking.CustomerId, 5);
                    await _context.SaveChangesAsync();
                    await SendConfirmationEmail(booking);
                }
                else
                {
                    payment.Status = "failed";
                    booking.Status = "cancelled";
                    await _context.SaveChangesAsync();
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentResult(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin đặt phòng.";
                return RedirectToAction("Index", "Rooms");
            }

            var payment = await _context.OnlinePayments
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync(p => p.BookingId == bookingId);
            
            var roomName = booking.Room?.RoomNumber ?? "N/A";
            if (booking.Room?.RoomType != null)
            {
                roomName = $"{booking.Room.RoomNumber} - {booking.Room.RoomType.TypeName}";
            }
            
            var model = new PaymentResultViewModel
            {
                IsSuccess = payment?.Status == "completed" || payment?.Status == "pending_deposit",
                Message = payment?.Status == "completed" ? "Thanh toán thành công! Đặt phòng đã được xác nhận." : 
                         payment?.Status == "pending_deposit" ? "Vui lòng thanh toán cọc theo hướng dẫn dưới đây." :
                         payment?.Status == "pending" ? "Đang xử lý thanh toán MoMo..." :
                         "Đang chờ thanh toán...",
                BookingCode = booking.BookingCode,
                Amount = payment?.Amount ?? booking.TotalAmount ?? 0,
                PaymentMethod = payment?.PaymentMethod ?? "cash",
                BookingDate = booking.CreatedAt ?? DateTime.Now,
                CheckInDate = booking.CheckInDate.ToDateTime(TimeOnly.MinValue),
                CheckOutDate = booking.CheckOutDate.ToDateTime(TimeOnly.MinValue),
                RoomName = roomName
            };

            return View(model);
        }

        private async Task SendDepositEmail(Booking booking, OnlineBookingViewModel model, decimal totalAmount, decimal depositAmount)
        {
            try
            {
                var subject = $"Hướng dẫn thanh toán cọc - {booking.BookingCode}";
                var body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h2 style='color: #667eea;'>🎉 Đặt phòng thành công!</h2>
                        <p>Xin chào <strong>{model.CustomerName}</strong>,</p>
                        <p>Cảm ơn bạn đã đặt phòng tại <strong>Khách sạn Resort Deluxe</strong>!</p>
                        
                        <div style='background: #f8f9fa; padding: 20px; border-radius: 10px; margin: 20px 0;'>
                            <h3>📋 Thông tin đặt phòng:</h3>
                            <ul>
                                <li>✅ Mã: <strong style='color: #667eea;'>{booking.BookingCode}</strong></li>
                                <li>🏨 Phòng: <strong>{model.RoomName} - {model.RoomType}</strong></li>
                                <li>📅 Nhận phòng: <strong>{model.CheckInDate:dd/MM/yyyy}</strong></li>
                                <li>📅 Trả phòng: <strong>{model.CheckOutDate:dd/MM/yyyy}</strong></li>
                                <li>💰 Tổng tiền: <strong>{totalAmount:N0} VNĐ</strong></li>
                                <li>💳 Tiền cọc 30%: <strong style='color: #28a745;'>{depositAmount:N0} VNĐ</strong></li>
                            </ul>
                        </div>
                        
                        <div style='background: #fff3cd; padding: 20px; border-radius: 10px;'>
                            <h3>🏦 Thông tin chuyển khoản:</h3>
                            <ul>
                                <li>🏦 Ngân hàng: <strong>Vietcombank</strong></li>
                                <li>💳 STK: <strong>1234567890</strong></li>
                                <li>👤 Chủ TK: <strong>KHACH SAN RESORT DELUXE</strong></li>
                                <li>📝 Nội dung: <strong style='color: #dc3545;'>{booking.BookingCode}</strong></li>
                                <li>💵 Số tiền: <strong>{depositAmount:N0} VNĐ</strong></li>
                            </ul>
                        </div>
                        
                        <p style='color: #dc3545;'><strong>⏰ Lưu ý:</strong> Chuyển khoản trong 24h để giữ phòng.</p>
                        <p>Trân trọng,<br><strong>Resort Deluxe</strong></p>
                    </div>
                ";

                await _emailSender.SendAsync(model.CustomerEmail, subject, body);
                Console.WriteLine($"✅ Email sent to {model.CustomerEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Email error: {ex.Message}");
            }
        }

        private async Task SendConfirmationEmail(Booking booking)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(booking.CustomerId);
                if (customer == null) return;

                var subject = $"Xác nhận đặt phòng - {booking.BookingCode}";
                var body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h2 style='color: #28a745;'>✅ Đặt phòng thành công!</h2>
                        <p>Xin chào <strong>{customer.FirstName} {customer.LastName}</strong>,</p>
                        <p>Đặt phòng của bạn đã được <strong>xác nhận</strong>.</p>
                        
                        <div style='background: #d4edda; padding: 20px; border-radius: 10px;'>
                            <ul>
                                <li>Mã: <strong>{booking.BookingCode}</strong></li>
                                <li>Nhận phòng: <strong>{booking.CheckInDate:dd/MM/yyyy}</strong></li>
                                <li>Trả phòng: <strong>{booking.CheckOutDate:dd/MM/yyyy}</strong></li>
                                <li>Tổng tiền: <strong>{booking.TotalAmount:N0} VNĐ</strong></li>
                            </ul>
                        </div>
                        
                        <p>🎉 <strong>+5 điểm tích lũy!</strong> Tổng: {customer.LoyaltyPoints ?? 0} điểm</p>
                        <p>Trân trọng,<br><strong>Resort Deluxe</strong></p>
                    </div>
                ";

                await _emailSender.SendAsync(customer.Email!, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Confirmation email error: {ex.Message}");
            }
        }

        private async Task AddLoyaltyPoints(int? customerId, int points)
        {
            if (customerId == null) return;

            try
            {
                var customer = await _context.Customers.FindAsync(customerId);
                if (customer != null)
                {
                    customer.LoyaltyPoints = (customer.LoyaltyPoints ?? 0) + points;
                    customer.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }
            catch { }
        }

        /// <summary>
        /// Helper method: Get User ID from username
        /// </summary>
        private async Task<int?> GetUserIdAsync(string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            return user?.UserId;
        }

        /// <summary>
        /// Helper method: Reload room info from database when returning View with errors
        /// </summary>
        private async Task ReloadRoomInfo(OnlineBookingViewModel model)
        {
            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.RoomId == model.RoomId);
            
            if (room != null)
            {
                model.RoomName = room.RoomNumber ?? "Unknown";
                model.RoomType = room.RoomType?.TypeName ?? "Standard Room";
                model.RoomPrice = room.RoomType?.BasePrice ?? 1500000;
                model.RoomImage = "/deluxe-assets/images/room-1.jpg";
                model.MaxOccupancy = room.RoomType?.MaxOccupancy ?? 2;
                Console.WriteLine($"✅ Reloaded room info: RoomId={model.RoomId}, Price={model.RoomPrice}");
            }
        }
    }
}

