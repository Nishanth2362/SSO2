using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SSO.Domain.Entities;
using System.Threading.Tasks;
using System.Linq;
using SSO.Application.Interfaces.Services;
using SSO.Infrastructure.Models;

namespace SSO.WebApplication.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMailService _mailService;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly DefaultSetting _defaultSetting;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            IMailService mailService,
            IEmailTemplateService emailTemplateService,
            Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _userManager = userManager;
            _mailService = mailService;
            _emailTemplateService = emailTemplateService;
            _defaultSetting = configuration.GetSection("DefaultSetting").Get<DefaultSetting>()!;
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ApplicationUser model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.Name = model.Name;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View("Profile", user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
            {
                TempData["ErrorMessage"] = "All password fields are required.";
                return RedirectToAction(nameof(Profile));
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "New passwords do not match.";
                return RedirectToAction(nameof(Profile));
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                var tokens = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["UserName"]   = user.Name ?? user.UserName ?? "User",
                    ["Email"]      = user.Email ?? string.Empty,
                    ["AppName"]    = _defaultSetting.ClientName,
                    ["TenantName"] = _defaultSetting.TenantName,
                    ["LogoUrl"]    = string.Empty
                };

                var emailTemplate = await _emailTemplateService.RenderAsync(SSO.Domain.Enums.EmailTriggerEvent.SecurityAlert, tokens);

                await _mailService.SendAsync(new SSO.Application.Requests.MailRequest
                {
                    To = new System.Collections.Generic.List<string> { user.Email! },
                    Subject = emailTemplate?.Subject ?? "Security Alert: Password Changed",
                    Body = emailTemplate?.Body ?? "Your password was recently changed. If this wasn't you, please contact support immediately."
                });

                TempData["SuccessMessage"] = "Password updated successfully!";
                return RedirectToAction(nameof(Profile));
            }

            TempData["ErrorMessage"] = string.Join("; ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Profile));
        }
    }
}
