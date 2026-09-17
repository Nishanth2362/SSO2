using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSO.Application.Interfaces.Services;
using System;
using System.Threading.Tasks;

namespace SSO.WebApplication.Controllers
{
    /// <summary>
    /// Serves the Security Management dashboard and user-specific security history.
    /// </summary>
    [Authorize]
    public class SecurityController : Controller
    {
        private readonly ISecurityEventService _securityEventService;

        public SecurityController(ISecurityEventService securityEventService)
        {
            _securityEventService = securityEventService;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // GET /Security
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var summary = await _securityEventService.GetSummaryAsync();
            var recentEvents = await _securityEventService.GetRecentAsync(200);

            ViewBag.Summary = summary;
            return View(recentEvents);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // GET /Security/UserHistory/{userId}
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<IActionResult> UserHistory(Guid userId, string userName)
        {
            var events = await _securityEventService.GetByUserAsync(userId, 100);
            
            ViewBag.UserId = userId;
            ViewBag.UserName = userName;
            
            return View(events);
        }
    }
}
