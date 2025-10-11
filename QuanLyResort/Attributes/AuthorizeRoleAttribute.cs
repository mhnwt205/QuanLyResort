using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using QuanLyResort.Models;
using System.Security.Claims;

namespace QuanLyResort.Attributes
{
    public class AuthorizeRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _allowedRoles;

        public AuthorizeRoleAttribute(params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new RedirectToActionResult("Login", "Account", new { area = "" });
                return;
            }

            var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(roleClaim))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", new { area = "" });
                return;
            }

            if (!_allowedRoles.Contains(roleClaim))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", new { area = "" });
                return;
            }
        }
    }

    public class AdminOnlyAttribute : AuthorizeRoleAttribute
    {
        public AdminOnlyAttribute() : base("Admin") { }
    }

    public class ManagerOrAdminAttribute : AuthorizeRoleAttribute
    {
        public ManagerOrAdminAttribute() : base("Admin", "Manager") { }
    }

    public class ReceptionistOrAboveAttribute : AuthorizeRoleAttribute
    {
        public ReceptionistOrAboveAttribute() : base("Admin", "Manager", "Receptionist") { }
    }

    public class CashierOrAboveAttribute : AuthorizeRoleAttribute
    {
        public CashierOrAboveAttribute() : base("Admin", "Manager", "Cashier") { }
    }

    public class HousekeepingOrAboveAttribute : AuthorizeRoleAttribute
    {
        public HousekeepingOrAboveAttribute() : base("Admin", "Manager", "Housekeeping") { }
    }
}
