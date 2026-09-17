using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using SSO.Domain.Entities;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using SSO.Application.Interfaces.Services;
using SSO.Infrastructure.Models;
using SSO.Shared.Wrapper.Mediator;
using SSO.Common.Constants.Application;
using SSO.Application.Features.ManagementTransactions.Commands.RecordChanges;

namespace SSO.WebApplication.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IMailService _mailService;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly DefaultSetting _defaultSetting;
        private readonly IAuditService _auditService;
        private readonly IMediator _mediator;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IMailService mailService,
            IEmailTemplateService emailTemplateService,
            Microsoft.Extensions.Configuration.IConfiguration configuration,
            IAuditService auditService,
            IMediator mediator)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _mailService = mailService;
            _emailTemplateService = emailTemplateService;
            _defaultSetting = configuration.GetSection("DefaultSetting").Get<DefaultSetting>()!;
            _auditService = auditService;
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var auditResult = await _auditService.GetCurrentUserTrailsAsync(user.Id.ToString());
            ViewBag.AuditLogs = auditResult.Succeeded 
                ? auditResult.Data 
                : new System.Collections.Generic.List<SSO.Application.Responses.Audit.AuditResponse>();

            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> DashboardProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var auditResult = await _auditService.GetCurrentUserTrailsAsync(user.Id.ToString());
            ViewBag.AuditLogs = auditResult.Succeeded 
                ? auditResult.Data 
                : new System.Collections.Generic.List<SSO.Application.Responses.Audit.AuditResponse>();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string name, string email, string phoneNumber)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var changedFields = new List<string>();
            if (!string.Equals(user.Name, name, System.StringComparison.Ordinal))
            {
                changedFields.Add("name");
                user.Name = name;
            }

            if (!string.Equals(user.PhoneNumber, phoneNumber, System.StringComparison.Ordinal))
            {
                changedFields.Add("phone_number");
                user.PhoneNumber = phoneNumber;
            }

            if (!string.IsNullOrWhiteSpace(email) && !email.Equals(user.Email, System.StringComparison.OrdinalIgnoreCase))
            {
                changedFields.Add("email");
                user.Email = email.Trim();
                user.EmailConfirmed = true; // Auto-confirm direct profile email updates
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);

                // Record modified fields on active RPAP transaction
                var txIdStr = Request.Cookies[ApplicationConstants.RpapCookies.TransactionId];
                if (System.Guid.TryParse(txIdStr, out var txId) && changedFields.Count > 0)
                {
                    await _mediator.Send(new RecordManagementTransactionChangesCommand
                    {
                        TransactionId = txId,
                        ChangedFields = changedFields
                    });
                }

                TempData["SuccessMessage"] = "Profile Updated Successfully";
                return RedirectToProfile();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            TempData["ErrorMessage"] = "Failed to Save Changes";

            return View("Profile", user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendEmailChangeVerification(string newEmail)
        {
            if (string.IsNullOrWhiteSpace(newEmail))
            {
                return Json(new { succeeded = false, message = "Email is required." });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { succeeded = false, message = "User not found." });

            var existingUser = await _userManager.FindByEmailAsync(newEmail.Trim());
            if (existingUser != null && existingUser.Id != user.Id)
            {
                return Json(new { succeeded = false, message = "This email is already in use by another account." });
            }

            var code = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail.Trim());
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Action(
                "ConfirmEmailChange",
                "Account",
                values: new { userId = user.Id, email = newEmail.Trim(), code = code },
                protocol: Request.Scheme);

            var tokens = new System.Collections.Generic.Dictionary<string, string>
            {
                ["UserName"]    = user.Name ?? user.UserName ?? "User",
                ["Email"]       = newEmail.Trim(),
                ["AppName"]     = _defaultSetting.ClientName,
                ["TenantName"]  = _defaultSetting.TenantName,
                ["CallbackUrl"] = System.Text.Encodings.Web.HtmlEncoder.Default.Encode(callbackUrl!),
                ["LogoUrl"]     = string.Empty
            };

            var emailTemplate = await _emailTemplateService.RenderAsync(SSO.Domain.Enums.EmailTriggerEvent.EmailConfirmation, tokens);

            var emailChangeRequest = new SSO.Application.Requests.MailRequest
            {
                To = new System.Collections.Generic.List<string> { newEmail.Trim() },
                Subject = !string.IsNullOrWhiteSpace(emailTemplate?.Subject) ? emailTemplate.Value.Subject : "Confirm your email change",
                Body = !string.IsNullOrWhiteSpace(emailTemplate?.Body) ? emailTemplate.Value.Body : $"Please confirm your email change by <a href='{System.Text.Encodings.Web.HtmlEncoder.Default.Encode(callbackUrl!)}'>clicking here</a>."
            };
            _ = BackgroundJob.Enqueue(() => _mailService.SendAsync(emailChangeRequest));

            return Json(new { succeeded = true });
        }

        [HttpGet]
        public async Task<IActionResult> CheckEmailChangeStatus(string newEmail)
        {
            if (string.IsNullOrWhiteSpace(newEmail))
            {
                return Json(new { verified = false });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { verified = false });

            bool verified = string.Equals(user.Email, newEmail.Trim(), StringComparison.OrdinalIgnoreCase);
            return Json(new { verified });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmailChange(string userId, string email, string code)
        {
            if (userId == null || email == null || code == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ChangeEmailAsync(user, email, decodedCode);

            if (!result.Succeeded)
            {
                ViewData["Message"] = "Error confirming your email change. The verification link might be expired or invalid.";
                return View("EmailConfirmed");
            }

            await _signInManager.RefreshSignInAsync(user);

            ViewData["Message"] = "Thank you for confirming your email change.";
            return View("EmailConfirmed");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
            {
                TempData["WarningMessage"] = "Invalid Input";
                return RedirectToProfile();
            }

            if (newPassword != confirmPassword)
            {
                TempData["WarningMessage"] = "Invalid Input";
                return RedirectToProfile();
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

                var passwordMailRequest = new SSO.Application.Requests.MailRequest
                {
                    To = new System.Collections.Generic.List<string> { user.Email! },
                    Subject = !string.IsNullOrWhiteSpace(emailTemplate?.Subject) ? emailTemplate.Value.Subject : "Security Alert: Password Changed",
                    Body = !string.IsNullOrWhiteSpace(emailTemplate?.Body) ? emailTemplate.Value.Body : "Your password was recently changed. If this wasn't you, please contact support immediately."
                };
                _ = BackgroundJob.Enqueue(() => _mailService.SendAsync(passwordMailRequest));

                // Record password change on active RPAP transaction
                var txIdStr = Request.Cookies[ApplicationConstants.RpapCookies.TransactionId];
                if (System.Guid.TryParse(txIdStr, out var txId))
                {
                    await _mediator.Send(new RecordManagementTransactionChangesCommand
                    {
                        TransactionId = txId,
                        ChangedFields = new List<string> { "password" }
                    });
                }

                TempData["SuccessMessage"] = "Password Changed Successfully";
                return RedirectToProfile();
            }

            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            TempData["ErrorMessage"] = !string.IsNullOrEmpty(errors) ? errors : "Failed to change password. Please check your current password.";
            return RedirectToProfile();
        }

        private IActionResult RedirectToProfile()
        {
            var referer = Request.Headers["Referer"].ToString();
            string targetAction = nameof(Profile);
            
            if (!string.IsNullOrEmpty(referer) && referer.Contains("DashboardProfile", StringComparison.OrdinalIgnoreCase))
            {
                targetAction = nameof(DashboardProfile);
            }

            var ssoMode = Request.Query["sso_mode"].ToString();
            if (string.IsNullOrEmpty(ssoMode))
            {
                ssoMode = Request.Cookies["SSO_ViewContext"];
            }

            if (!string.IsNullOrEmpty(ssoMode))
            {
                return RedirectToAction(targetAction, new { sso_mode = ssoMode });
            }
            return RedirectToAction(targetAction);
        }
    }
}
