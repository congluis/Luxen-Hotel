using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;

namespace LuxenHotel.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public abstract class AdminBaseController : Controller
    {
        public readonly ILogger<AdminBaseController> _logger;

        // Constructor để inject ILogger
        protected AdminBaseController(ILogger<AdminBaseController> logger)
        {
            _logger = logger;
        }

        // Thiết lập tiêu đề trang
        protected void SetPageTitle(string title)
        {
            ViewBag.Title = $"{title}";
        }

        // Ghi log thông tin
        protected void LogInfo(string message)
        {
            _logger.LogInformation($"[Admin] {User.Identity.Name}: {message}");
        }

        // Ghi log lỗi
        protected void LogError(string message, Exception ex = null)
        {
            _logger.LogError(ex, $"[Admin] {User.Identity.Name}: {message}");
        }

        // Thêm thông báo cho người dùng
        protected void AddNotification(string message, NotificationType type)
        {
            TempData["NotificationMessage"] = message;
            TempData["NotificationType"] = type.ToString().ToLower();
        }

        // Kiểm tra quyền bổ sung
        protected bool HasPermission(string permission)
        {
            return User.HasClaim(c => c.Type == "Permission" && c.Value == permission);
        }

        // Xử lý lỗi chung
        protected IActionResult HandleError(string errorMessage, Exception ex = null)
        {
            LogError(errorMessage, ex);
            AddNotification(errorMessage, NotificationType.Error);
            return RedirectToAction("Index", "Dashboard");
        }

        protected void HandleInvalidModelState()
        {
            AddNotification("Invalid input data", NotificationType.Error);
            LogInfo("Invalid input data for creating accommodation");

            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            foreach (var error in errors)
            {
                AddNotification(error, NotificationType.Error);
            }
        }
    }

    // Enum để định nghĩa loại thông báo
    public enum NotificationType
    {
        Success,
        Error,
        Warning,
        Info
    }
}