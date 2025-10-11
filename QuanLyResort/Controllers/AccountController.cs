using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyResort.Models;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using QuanLyResort.Services.Interfaces;

namespace QuanLyResort.Controllers
{
    public class AccountController : Controller
    {
        private readonly ResortDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IEmailSender _emailSender;

        public AccountController(ResortDbContext context, IMemoryCache cache, IEmailSender emailSender)
        {
            _context = context;
            _cache = cache;
            _emailSender = emailSender;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập tài khoản và mật khẩu.");
                return View();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == username && (u.IsActive ?? true));

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản không tồn tại hoặc bị khóa.");
                return View();
            }

            // So sánh hash BCrypt (ưu tiên). Nếu DB đang lưu plain text cũ, chấp nhận tạm thời.
            bool passwordOk;
            bool needsRehash = false;
            try
            {
                passwordOk = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }
            catch
            {
                // Fallback: Kiểm tra plain text hoặc SHA256
                passwordOk = string.Equals(user.PasswordHash, password);
                needsRehash = passwordOk; // Nếu đúng plain text, cần rehash
            }
            if (!passwordOk)
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu không đúng.");
                return View();
            }

            // Tự động chuyển sang BCrypt nếu đang dùng plain text
            if (needsRehash)
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Đã tự động chuyển mật khẩu của user '{user.Username}' sang BCrypt hash");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "Customer")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            // Redirect by role
            var role = user.Role?.RoleName ?? string.Empty;
            if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, "Receptionist", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, "Cashier", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, "Housekeeping", StringComparison.OrdinalIgnoreCase))
            {
                return Redirect("/Admin/Dashboard");
            }

            return Redirect("/Customer");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(
            string username, string password,
            string firstName, string lastName,
            string? email, string? phone, string? address,
            string? nationality, string? idCardNumber, string? passportNumber,
            DateTime? dateOfBirth, string? gender)
        {
            // Validation cơ bản
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)
                || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập đầy đủ thông tin bắt buộc (Email, Mật khẩu, Họ, Tên).");
                return View();
            }

            // Đảm bảo username = email (quan trọng để liên kết User-Customer)
            username = email;

            // Validation email format
            if (!email.Contains("@") || !email.Contains("."))
            {
                ModelState.AddModelError(string.Empty, "Email không hợp lệ.");
                return View();
            }

            // Validation password length
            if (password.Length < 6)
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu phải có ít nhất 6 ký tự.");
                return View();
            }

            // Validation phone (nếu có)
            if (!string.IsNullOrWhiteSpace(phone))
            {
                phone = phone.Trim();
                if (phone.Length < 10 || phone.Length > 11 || !phone.All(char.IsDigit))
                {
                    ModelState.AddModelError(string.Empty, "Số điện thoại phải có 10-11 chữ số.");
                    return View();
                }
            }

            // Kiểm tra email đã tồn tại chưa
            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                ModelState.AddModelError(string.Empty, "Email này đã được đăng ký. Vui lòng sử dụng email khác.");
                return View();
            }

            // Kiểm tra email trong Customers (tránh duplicate)
            if (await _context.Customers.AnyAsync(c => c.Email == email))
            {
                ModelState.AddModelError(string.Empty, "Email này đã tồn tại trong hệ thống. Vui lòng sử dụng email khác.");
                return View();
            }

            try
            {
                // Hash mật khẩu bằng BCrypt
                var hash = BCrypt.Net.BCrypt.HashPassword(password);
                var roleCustomer = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Customer");

                // Tạo User mới (username = email)
                var newUser = new User
                {
                    Username = username, // = email
                    PasswordHash = hash,
                    RoleId = roleCustomer?.RoleId,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // Tạo Customer profile tương ứng
                var customer = new Customer
                {
                    CustomerCode = $"CUST-{DateTime.Now:yyyyMMddHHmmss}",
                    FirstName = firstName.Trim(),
                    LastName = lastName.Trim(),
                    Email = email, // PHẢI GIỐNG username để liên kết
                    Phone = phone,
                    Address = address,
                    Nationality = nationality,
                    IdCardNumber = idCardNumber,
                    PassportNumber = passportNumber,
                    DateOfBirth = dateOfBirth.HasValue ? DateOnly.FromDateTime(dateOfBirth.Value) : null,
                    Gender = gender,
                    CustomerType = "individual",
                    LoyaltyPoints = 0,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập bằng email và mật khẩu đã đăng ký.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi đăng ký. Vui lòng thử lại sau.");
                Console.WriteLine($"[ERROR] Register failed: {ex.Message}");
                return View();
            }
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/Customer");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Redirect("/Account/Login");
            if (!int.TryParse(userIdStr, out var userId)) return Redirect("/Account/Login");

            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
            var customer = await _context.Customers
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync(c => c.Email == user!.Username || c.Phone == user!.Username);

            ViewBag.User = user;
            return View(customer);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(
            string firstName, string lastName,
            string? phone, string? address,
            DateTime? dateOfBirth, string? gender)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Redirect("/Account/Login");
            if (!int.TryParse(userIdStr, out var userId)) return Redirect("/Account/Login");

            // Validation cơ bản
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                TempData["ErrorMessage"] = "Họ và tên không được để trống.";
                return RedirectToAction("Profile");
            }

            // Validation phone (nếu có)
            if (!string.IsNullOrWhiteSpace(phone))
            {
                phone = phone.Trim();
                if (phone.Length < 10 || phone.Length > 11 || !phone.All(char.IsDigit))
                {
                    TempData["ErrorMessage"] = "Số điện thoại phải có 10-11 chữ số.";
                    return RedirectToAction("Profile");
                }
            }

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy tài khoản.";
                    return RedirectToAction("Profile");
                }

                // Tìm customer theo email của user
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Email == user.Username);

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng.";
                    return RedirectToAction("Profile");
                }

                // Cập nhật thông tin customer
                customer.FirstName = firstName.Trim();
                customer.LastName = lastName.Trim();
                customer.Phone = phone;
                customer.Address = address?.Trim();
                customer.DateOfBirth = dateOfBirth.HasValue ? DateOnly.FromDateTime(dateOfBirth.Value) : null;
                customer.Gender = gender;
                customer.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật. Vui lòng thử lại sau.";
                Console.WriteLine($"[ERROR] UpdateProfile failed: {ex.Message}");
                return RedirectToAction("Profile");
            }
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login");
            if (!int.TryParse(userIdStr, out var userId)) return RedirectToAction("Login");

            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập đầy đủ thông tin.");
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản không tồn tại.");
                return View();
            }

            bool passwordOk;
            try { passwordOk = BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash); }
            catch { passwordOk = string.Equals(user.PasswordHash, currentPassword); }
            if (!passwordOk)
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu hiện tại không đúng.");
                return View();
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công.";
            return RedirectToAction("Profile");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập email.");
                return View();
            }

            // Tìm user theo username = email
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == email);

            // Kiểm tra user có tồn tại không
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Email không tồn tại trong hệ thống.");
                return View();
            }

            // Kiểm tra tài khoản có active không
            if (user.IsActive == false)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
                return View();
            }

            // Tạo OTP và lưu cache 10 phút
            var otp = new Random().Next(100000, 999999).ToString();
            var cacheKey = $"pwd-otp-{email.ToLower()}";
            _cache.Set(cacheKey, otp, TimeSpan.FromMinutes(10));

            // Gửi email với template đẹp hơn
            var subject = "Mã xác thực đặt lại mật khẩu - Resort Management System";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #333; border-bottom: 2px solid #4CAF50; padding-bottom: 10px;'>
                        Đặt lại mật khẩu
                    </h2>
                    <p style='font-size: 16px; color: #555;'>Xin chào <strong>{user.Username}</strong>,</p>
                    <p style='font-size: 16px; color: #555;'>
                        Bạn đã yêu cầu đặt lại mật khẩu. Vui lòng sử dụng mã OTP bên dưới để tiếp tục:
                    </p>
                    <div style='background-color: #f5f5f5; padding: 20px; text-align: center; margin: 20px 0; border-radius: 5px;'>
                        <span style='font-size: 32px; font-weight: bold; color: #4CAF50; letter-spacing: 5px;'>{otp}</span>
                    </div>
                    <p style='font-size: 14px; color: #888;'>
                        ⏰ Mã OTP có hiệu lực trong <strong>10 phút</strong>.
                    </p>
                    <p style='font-size: 14px; color: #888;'>
                        ⚠️ Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.
                    </p>
                    <hr style='border: none; border-top: 1px solid #ddd; margin: 30px 0;'>
                    <p style='font-size: 12px; color: #aaa; text-align: center;'>
                        © 2025 Resort Management System. All rights reserved.
                    </p>
                </div>
            ";
            
            try
            {
                await _emailSender.SendAsync(email, subject, body);
                TempData["SuccessMessage"] = "Đã gửi mã OTP tới email của bạn. Vui lòng kiểm tra hộp thư.";
                return RedirectToAction("ResetPassword", new { email });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Không thể gửi email. Vui lòng thử lại sau.");
                // Log error để debug
                Console.WriteLine($"[ERROR] Send email failed: {ex.Message}");
                return View();
            }
        }

        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string email, string otp, string newPassword)
        {
            ViewBag.Email = email;

            // Validation input
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otp) || string.IsNullOrWhiteSpace(newPassword))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập đầy đủ thông tin.");
                return View();
            }

            // Validation mật khẩu mới
            if (newPassword.Length < 6)
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu mới phải có ít nhất 6 ký tự.");
                return View();
            }

            // Kiểm tra OTP từ cache
            var cacheKey = $"pwd-otp-{email.ToLower()}";
            if (!_cache.TryGetValue<string>(cacheKey, out var cachedOtp))
            {
                ModelState.AddModelError(string.Empty, "Mã OTP đã hết hạn. Vui lòng yêu cầu mã mới.");
                return View();
            }

            // So sánh OTP (loại bỏ khoảng trắng)
            if (!string.Equals(cachedOtp?.Trim(), otp?.Trim(), StringComparison.Ordinal))
            {
                ModelState.AddModelError(string.Empty, "Mã OTP không chính xác. Vui lòng kiểm tra lại.");
                return View();
            }

            // Tìm user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Không tìm thấy tài khoản với email này.");
                return View();
            }

            // Kiểm tra tài khoản có active không
            if (user.IsActive == false)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
                return View();
            }

            // Cập nhật mật khẩu mới
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // Xóa OTP khỏi cache
            _cache.Remove(cacheKey);

            TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập với mật khẩu mới.";
            return RedirectToAction("Login");
        }
    }
}


