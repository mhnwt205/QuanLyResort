using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using QuanLyResort.Models;

namespace QuanLyResort.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        // Show selection page for Admin or Customer
        return View();
    }

    public IActionResult Admin()
    {
        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
    }

    public IActionResult Customer()
    {
        return RedirectToAction("Index", "Home", new { area = "Customer" });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
