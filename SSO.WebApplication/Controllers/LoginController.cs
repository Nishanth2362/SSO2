using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using SSO.Application.Interfaces.Services;
using SSO.Application.Interfaces.Services.Features;
using SSO.Domain.Enums;
using SSO.Application.Requests;
using SSO.Application.Requests.Identity;
using SSO.Domain.Entities;
using SSO.Infrastructure.Models;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.EntityFrameworkCore;

namespace SSO.WebApplication.Controllers
{
    [AllowAnonymous]
    public class LoginController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAccessControlService _accessControlService;
        private readonly IMailService _mailService;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IConfiguration _configuration;
        private readonly DefaultSetting _defaultSetting;
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly IOpenIddictAuthorizationManager _authorizationManager;
        private readonly IRoleService _roleService;
        private readonly ITwoFactorService _twoFactorService;
        private readonly ISecurityEventService _securityEventService;
        private readonly IMemoryCache _memoryCache;
        private readonly IAntiBotSecurityService _antiBotService;
        private readonly ILogger<LoginController> _logger;
        private string RoleName;

        public LoginController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IAccessControlService accessControlService,
            IMailService mailService,
            IEmailTemplateService emailTemplateService,
            IConfiguration configuration,
            IOpenIddictApplicationManager applicationManager,
            IOpenIddictAuthorizationManager authorizationManager,
            IRoleService roleService,
            ITwoFactorService twoFactorService,
            ISecurityEventService securityEventService,
            IMemoryCache memoryCache,
            IAntiBotSecurityService antiBotService,
            ILogger<LoginController> logger)
        {
            _signInManager          = signInManager;
            _userManager            = userManager;
            _accessControlService   = accessControlService;
            _mailService            = mailService;
            _emailTemplateService   = emailTemplateService;
            _configuration          = configuration;
            _defaultSetting         = configuration.GetSection("DefaultSetting").Get<DefaultSetting>()!;
            _applicationManager     = applicationManager;
            _authorizationManager   = authorizationManager;
            _roleService            = roleService;
            _twoFactorService       = twoFactorService;
            _securityEventService   = securityEventService;
            _memoryCache            = memoryCache;
            _antiBotService         = antiBotService;
            _logger                 = logger;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // GET /Login
        // ─────────────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Index(string? returnUrl = null)
        {
            await SetTenantBrandingAsync(returnUrl);
            await SetLoginMethodViewBagAsync(returnUrl);

            ViewData["ReturnUrl"] = returnUrl;
            var registeredEmail = TempData["RegisteredEmail"]?.ToString() ?? string.Empty;
            return View(new TokenRequest 
            { 
                ReturnUrl = returnUrl ?? string.Empty,
                UserName = registeredEmail
            });
        }

        // ─────────────────────────────────────────────────────────────────────────
        // POST /Login  — multi-step authentication engine
        // ─────────────────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(TokenRequest request)
        {
            ViewData["ReturnUrl"] = request.ReturnUrl;

            var clientId    = ResolveClientId(request.ReturnUrl);
            var tenantId    = ResolveTenantId(request.ReturnUrl);
            var branding    = await _accessControlService.GetTenantByClientIdAsync(clientId!, tenantId);
            var client      = await _applicationManager.FindByClientIdAsync(clientId!) as ApplicationClient;
            var loginMethod = client?.AllowedLoginMethod ?? LoginMethod.CredentialsOnly;

            await SetTenantBrandingAsync(request.ReturnUrl);
            SetLoginMethodViewBag(loginMethod);
            ViewBag.AllowRegistration = client?.AllowPublicRegistration ?? false;

            var isMobileFlow = loginMethod == LoginMethod.MobileOtpOnly
                               || (loginMethod == LoginMethod.Both && request.LoginFlow?.Equals("mobile", StringComparison.OrdinalIgnoreCase) == true);

            // ── MOBILE OTP FLOW ───────────────────────────────────────────────────────
            if (isMobileFlow)
            {
                return await HandleMobileOtpFlowAsync(request, clientId!, branding, isMobileFlow);
            }

            // ── CREDENTIALS FLOW ─────────────────────────────────────────────────────
            return await HandleCredentialsFlowAsync(request, clientId!, branding, client);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Mobile OTP flow handler
        // ─────────────────────────────────────────────────────────────────────────

        private async Task<IActionResult> HandleMobileOtpFlowAsync(
            TokenRequest request,
            string clientId,
            Domain.Entities.Tenants? branding,
            bool isMobileFlow)
        {
            request.LoginFlow = "mobile";

            if (request.Step <= 1)
            {
                // ── Step 1: Receive phone/email → send OTP ──────────────────────
                if (string.IsNullOrWhiteSpace(request.PhoneOrEmail))
                {
                    ModelState.AddModelError(nameof(request.PhoneOrEmail), "Email or phone number is required.");
                    TempData["ErrorMessage"] = "Please enter your email or phone number.";
                    request.ErrorMessage = "Please enter your email or phone number.";
                    request.Step = 1;
                    return View(request);
                }

                var user = await FindUserByPhoneOrEmailAsync(request.PhoneOrEmail);
                if (user == null)
                {
                    ModelState.AddModelError(nameof(request.PhoneOrEmail), "No account found with that email or phone number.");
                    request.Step = 1;
                    return View(request);
                }

                // Access control check before sending OTP
                var accessError = await _accessControlService.ValidateUserAccessAsync(user, clientId);
                if (accessError != null)
                {
                    ModelState.AddModelError(string.Empty, accessError);
                    request.ErrorMessage = accessError;
                    request.Step = 1;

                    await _securityEventService.LogAsync(new SecurityEventContext(
                        HttpContext, user.Id, user.UserName, clientId,
                        SecurityEventType.AccessDenied, AttemptNumber: 1));

                    return View(request);
                }

                await _twoFactorService.SendOtpAsync(user, branding, EmailTriggerEvent.MobileOtpCode);

                request.PhoneOrEmail = user.Email; // store confirmed email for step 2 lookup
                request.Step = 2;
                ModelState.Clear();
                request.ErrorMessage = string.Empty;
                TempData["InfoMessage"] = "A verification code has been sent to your registered email address.";
                return View(request);
            }

            if (request.Step == 2)
            {
                // ── Step 2: Validate OTP → sign in ──────────────────────────────
                var user = await FindUserByPhoneOrEmailAsync(request.PhoneOrEmail);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Session expired. Please start again.");
                    request.Step = 1;
                    return View(request);
                }

                if (string.IsNullOrWhiteSpace(request.TwoFactorCode))
                {
                    ModelState.AddModelError(nameof(request.TwoFactorCode), "Verification code is required.");
                    TempData["ErrorMessage"] = "Please enter the verification code.";
                    request.ErrorMessage = "Please enter the verification code.";
                    request.Step = 2;
                    return View(request);
                }

                var otpResult = await _twoFactorService.ValidateOtpAsync(user, request.TwoFactorCode);
                if (!otpResult.IsValid)
                {
                    await _userManager.AccessFailedAsync(user);
                    var failCount = (user.AccessFailedCount + 1);

                    await _securityEventService.LogAsync(new SecurityEventContext(
                        HttpContext, user.Id, user.UserName, clientId,
                        SecurityEventType.OtpFailed,
                        AttemptNumber: failCount,
                        IsBlocked: false));

                    request.ErrorMessage = otpResult.Error ?? "Invalid verification code.";
                    ModelState.AddModelError(nameof(request.TwoFactorCode), request.ErrorMessage);
                    request.Step = 2;
                    request.TwoFactorCode = string.Empty;
                    TempData["ErrorMessage"] = otpResult.Error ?? "Invalid Code";
                    return View(request);
                }

                // Success
                await _userManager.ResetAccessFailedCountAsync(user);
                await _securityEventService.LogAsync(new SecurityEventContext(
                    HttpContext, user.Id, user.UserName, clientId,
                    SecurityEventType.LoginSuccess, AttemptNumber: 1));

                await _signInManager.SignInAsync(user, request.RememberMe);
                TempData["SuccessMessage"] = "Login Successful";

                if (!string.IsNullOrEmpty(request.ReturnUrl) && IsUrlLocalOrSameHost(request.ReturnUrl))
                    return Redirect(request.ReturnUrl);

                return RedirectToAction("Index", "Home");
            }

            return View(request);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Credentials flow handler
        // ─────────────────────────────────────────────────────────────────────────

        private async Task<IActionResult> HandleCredentialsFlowAsync(
            TokenRequest request,
            string clientId,
            Domain.Entities.Tenants? branding,
            ApplicationClient? client)
        {
            request.Step = request.Step <= 1 ? 1 : request.Step;
            request.UserName = request.UserName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(request.UserName))
            {
                ModelState.AddModelError(nameof(request.UserName), "Email or username is required.");
                TempData["ErrorMessage"] = "Please enter your email or username.";
                request.ErrorMessage = "Please enter your email or username.";
                request.Step = 1;
                return View(request);
            }

            if (!ModelState.IsValid)
            {
                request.Step = string.IsNullOrWhiteSpace(request.UserName) ? 1 : request.Step;
                var firstErr = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                if (!string.IsNullOrEmpty(firstErr))
                {
                    TempData["ErrorMessage"] = firstErr;
                }
                return View(request);
            }

            var user = await FindUserByIdentifierAsync(request.UserName);
            if (user == null)
            {
                ModelState.AddModelError(nameof(request.UserName), "Account not found.");
                request.ErrorMessage = "Account not found.";
                TempData["ErrorMessage"] = "Account not found.";
                request.Step = 1;
                return View(request);
            }

            if (request.Step == 1)
            {
                // Just confirmed identity — move to password step
                request.Step = 2;
                request.Password = string.Empty;
                request.ErrorMessage = string.Empty;
                ModelState.Clear();
                return View(request);
            }

            // ── Step 2: Password check ────────────────────────────────────────────

            if (request.Step == 2)
            {
                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    ModelState.AddModelError(nameof(request.Password), "Password is required.");
                    TempData["ErrorMessage"] = "Please enter your password.";
                    request.ErrorMessage = "Please enter your password.";
                    request.Step = 2;
                    return View(request);
                }

                bool wasLockedOut = await _userManager.IsLockedOutAsync(user);
                var checkResult   = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

                if (checkResult.Succeeded)
                {
                    // Access control validation
                    var accessError = await _accessControlService.ValidateUserAccessAsync(user, clientId);
                    if (accessError != null)
                    {
                        ModelState.AddModelError(string.Empty, accessError);
                        request.ErrorMessage = accessError;
                        request.Password     = string.Empty;
                        request.Step         = 2;
                        TempData["ErrorMessage"] = "Access Denied";

                        await _securityEventService.LogAsync(new SecurityEventContext(
                            HttpContext, user.Id, user.UserName, clientId,
                            SecurityEventType.AccessDenied, AttemptNumber: 1));

                        return View(request);
                    }

                    // Check 2FA requirement
                    bool needs2FA = (client?.Require2FA ?? false) || user.TwoFactorEnabled;
                    if (needs2FA)
                    {
                        await _twoFactorService.SendOtpAsync(user, branding, EmailTriggerEvent.TwoFactorCode);

                        // Set secure 2FA pending state cookie to prevent Step 3 tampering
                        Response.Cookies.Append("SSO.2FA.Pending", $"{user.Id}|{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}", new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = Request.IsHttps,
                            SameSite = SameSiteMode.Lax,
                            Expires = DateTimeOffset.UtcNow.AddMinutes(10)
                        });

                        request.Step          = 3;
                        request.Password      = string.Empty;
                        request.ErrorMessage  = string.Empty;
                        ModelState.Clear();
                        TempData["InfoMessage"] = "A verification code has been sent to your email address.";
                        return View(request);
                    }

                    // No 2FA — sign in directly
                    await _securityEventService.LogAsync(new SecurityEventContext(
                        HttpContext, user.Id, user.UserName, clientId,
                        SecurityEventType.LoginSuccess, AttemptNumber: 1));

                    await _signInManager.SignInAsync(user, request.RememberMe);
                    TempData["SuccessMessage"] = "Login Successful";

                    if (!string.IsNullOrEmpty(request.ReturnUrl) && IsUrlLocalOrSameHost(request.ReturnUrl))
                        return Redirect(request.ReturnUrl);

                    return RedirectToAction("Index", "Home");
                }

                // ── Password failed ───────────────────────────────────────────────

                // Lock notification email (only fire on fresh lockout)
                if (checkResult.IsLockedOut && !wasLockedOut)
                {
                    await SendLockoutEmailAsync(user, branding);

                    await _securityEventService.LogAsync(new SecurityEventContext(
                        HttpContext, user.Id, user.UserName, clientId,
                        SecurityEventType.AccountLocked,
                        AttemptNumber: Math.Max(1, user.AccessFailedCount),
                        IsBlocked: true));
                }
                else if (!checkResult.IsLockedOut)
                {
                    // Log all failed attempts (attempt 1, 2, 3...)
                    await _securityEventService.LogAsync(new SecurityEventContext(
                        HttpContext, user.Id, user.UserName, clientId,
                        SecurityEventType.PasswordFailed,
                        AttemptNumber: Math.Max(1, user.AccessFailedCount)));
                }

                request.ErrorMessage = await BuildSignInErrorMessageAsync(user, checkResult);
                ModelState.AddModelError(string.Empty, request.ErrorMessage);
                TempData["ErrorMessage"] = checkResult.IsLockedOut || checkResult.IsNotAllowed
                    ? "Access Denied"
                    : "Invalid Password";
                request.Step = 2;
                request.Password = string.Empty;
                return View(request);
            }

            // ── Step 3: 2FA OTP validation ────────────────────────────────────────

            if (request.Step == 3)
            {
                // Strictly verify that Step 2 was completed by checking the SSO.2FA.Pending cookie
                var pending2FA = Request.Cookies["SSO.2FA.Pending"];
                var isPendingValid = false;
                if (!string.IsNullOrWhiteSpace(pending2FA))
                {
                    var pendingParts = pending2FA.Split('|');
                    if (pendingParts.Length == 2 && pendingParts[0] == user.Id.ToString() && long.TryParse(pendingParts[1], out var pendingTime) && (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - pendingTime) <= 600)
                    {
                        isPendingValid = true;
                    }
                }

                if (!isPendingValid)
                {
                    Response.Cookies.Delete("SSO.2FA.Pending");
                    ModelState.AddModelError(string.Empty, "Verification session expired or invalid. Please sign in again.");
                    TempData["ErrorMessage"] = "Verification session expired or invalid. Please sign in again.";
                    request.Step = 1;
                    request.Password = string.Empty;
                    request.TwoFactorCode = string.Empty;
                    return View(request);
                }

                if (string.IsNullOrWhiteSpace(request.TwoFactorCode))
                {
                    ModelState.AddModelError(nameof(request.TwoFactorCode), "Verification code is required.");
                    TempData["ErrorMessage"] = "Please enter the verification code.";
                    request.ErrorMessage = "Please enter the verification code.";
                    request.Step = 3;
                    return View(request);
                }

                var otpResult = await _twoFactorService.ValidateOtpAsync(user, request.TwoFactorCode);

                if (!otpResult.IsValid)
                {
                    await _userManager.AccessFailedAsync(user);
                    var failCount = (user.AccessFailedCount + 1);

                    await _securityEventService.LogAsync(new SecurityEventContext(
                        HttpContext, user.Id, user.UserName, clientId,
                        SecurityEventType.OtpFailed,
                        AttemptNumber: failCount));

                    request.ErrorMessage = otpResult.Error ?? "Invalid verification code.";
                    ModelState.AddModelError(nameof(request.TwoFactorCode), request.ErrorMessage);
                    request.TwoFactorCode = string.Empty;
                    request.Step = 3;
                    TempData["ErrorMessage"] = otpResult.Error ?? "Invalid Code";
                    return View(request);
                }

                // 2FA passed → clear pending cookie and sign in
                Response.Cookies.Delete("SSO.2FA.Pending");
                await _userManager.ResetAccessFailedCountAsync(user);

                await _securityEventService.LogAsync(new SecurityEventContext(
                    HttpContext, user.Id, user.UserName, clientId,
                    SecurityEventType.LoginSuccess, AttemptNumber: 1));

                await _signInManager.SignInAsync(user, request.RememberMe);
                TempData["SuccessMessage"] = "Login Successful";

                if (!string.IsNullOrEmpty(request.ReturnUrl) && IsUrlLocalOrSameHost(request.ReturnUrl))
                    return Redirect(request.ReturnUrl);

                return RedirectToAction("Index", "Home");
            }

            return View(request);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // POST /Login/ResendOtp — re-sends OTP for current step (rate-limited by UI)
        // ─────────────────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOtp(string? identifier, string? returnUrl, string flow = "credentials")
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return Json(new { success = false, message = "Identifier is required." });

            var clientId = ResolveClientId(returnUrl);
            var branding = await _accessControlService.GetTenantByClientIdAsync(clientId!, ResolveTenantId(returnUrl));

            ApplicationUser? user = flow == "mobile"
                ? await FindUserByPhoneOrEmailAsync(identifier)
                : await FindUserByIdentifierAsync(identifier);

            if (user == null)
                return Json(new { success = false, message = "User not found." });

            var trigger = flow == "mobile"
                ? EmailTriggerEvent.MobileOtpCode
                : EmailTriggerEvent.TwoFactorCode;

            await _twoFactorService.SendOtpAsync(user, branding, trigger);
            return Json(new { success = true, message = "A new verification code has been sent to your registered email address." });
        }

        // ─────────────────────────────────────────────────────────────────────────
        // VerifyUser (AJAX)
        // ─────────────────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyUser(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return Json(new { exists = false });
            var user = await FindUserByIdentifierAsync(username);
            return Json(new { exists = user != null });
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Logout
        // ─────────────────────────────────────────────────────────────────────────

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _signInManager.SignOutAsync();

                foreach (var cookieName in SSO.Common.Constants.Application.ApplicationConstants.RpapCookies.AllRpapCookies)
                {
                    Response.Cookies.Delete(cookieName);
                }

                TempData["SuccessMessage"] = "Logout Successful";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception during logout");
            }

            return RedirectToAction(nameof(Index));
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Register — Client-managed public registration with Email OTP & Anti-Bot
        // ─────────────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Register(string? returnUrl = null)
        {
            if (string.IsNullOrEmpty(returnUrl))
            {
                TempData["ErrorMessage"] = "Registration must be initiated from a client application OAuth flow.";
                return RedirectToAction(nameof(Index));
            }

            var clientId = ResolveClientId(returnUrl);
            var tenantId = ResolveTenantId(returnUrl);
            var tenant   = await _accessControlService.GetTenantByClientIdAsync(clientId!, tenantId);
            var client   = await _applicationManager.FindByClientIdAsync(clientId!) as ApplicationClient;

            if (client == null)
            {
                TempData["ErrorMessage"] = "Invalid client application configuration. You cannot register.";
                return RedirectToAction(nameof(Index), new { returnUrl });
            }

            if (!client.AllowPublicRegistration)
            {
                TempData["ErrorMessage"] = "Public registration is not available for this application. Please contact your administrator for an invitation.";
                return RedirectToAction(nameof(Index), new { returnUrl });
            }

            ViewBag.TenantBranding = tenant;
            ViewBag.ClientName = client.DisplayName;

            var timeLockToken = _antiBotService.GenerateTimeLockToken();

            return View(new RegisterRequest
            {
                ReturnUrl          = returnUrl,
                ClientId           = clientId!,
                TenantId           = tenant?.Id ?? Guid.Empty,
                Step               = 1,
                FormTimestampToken = timeLockToken
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var clientId = ResolveClientId(request.ReturnUrl) ?? request.ClientId;
            var tenantId = ResolveTenantId(request.ReturnUrl) ?? request.TenantId;
            var tenant   = await _accessControlService.GetTenantByClientIdAsync(clientId, tenantId);
            var client   = await _applicationManager.FindByClientIdAsync(clientId) as ApplicationClient;

            ViewBag.TenantBranding = tenant;
            ViewBag.ClientName = client?.DisplayName;

            if (client == null || !client.AllowPublicRegistration)
            {
                ModelState.AddModelError(string.Empty, "Public registration is not available for this application.");
                request.Step = 1;
                request.FormTimestampToken = _antiBotService.GenerateTimeLockToken();
                return View(request);
            }

            // ── Step 0: Rate Limiter (IP Throttling) ─────────────────────────
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var ipRateLimitKey = $"ratelimit_reg_ip_{remoteIp}";
            var ipAttempts = _memoryCache.GetOrCreate(ipRateLimitKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return 0;
            });

            if (ipAttempts >= 10)
            {
                _logger.LogWarning("Registration rate limit exceeded for IP {Ip}", remoteIp);
                ModelState.AddModelError(string.Empty, "Too many registration attempts. Please wait 5 minutes and try again.");
                request.Step = 1;
                request.FormTimestampToken = _antiBotService.GenerateTimeLockToken();
                return View(request);
            }
            _memoryCache.Set(ipRateLimitKey, ipAttempts + 1, TimeSpan.FromMinutes(5));

            // ── Step 1: Honeypot Trap Check ──────────────────────────────────
            if (_antiBotService.IsHoneypotTriggered(request.Website_Hp))
            {
                _logger.LogWarning("Registration bot honeypot tripped from IP {Ip}", remoteIp);
                ModelState.AddModelError(string.Empty, "Invalid registration submission. Please try again.");
                request.Step = 1;
                request.FormTimestampToken = _antiBotService.GenerateTimeLockToken();
                return View(request);
            }

            // ── Step 2: Time-Lock (HMAC) Validation (<2s bot, >30min expired) ─
            if (!_antiBotService.ValidateTimeLock(request.FormTimestampToken, out var timeLockError))
            {
                ModelState.AddModelError(string.Empty, timeLockError ?? "Security verification failed. Please try again.");
                request.Step = 1;
                request.FormTimestampToken = _antiBotService.GenerateTimeLockToken();
                return View(request);
            }

            // ── Step 3: Email Format Validation (Strict RFC 5322 Syntax) ──────
            if (!_antiBotService.IsValidEmailSyntax(request.Email))
            {
                ModelState.AddModelError(nameof(request.Email), "Please enter a valid email address format.");
                request.Step = 1;
                request.FormTimestampToken = _antiBotService.GenerateTimeLockToken();
                return View(request);
            }

            // ── Step 4: Email Domain Check (75,000+ Disposable Domains + DNS MX)
            if (_antiBotService.IsDisposableEmail(request.Email))
            {
                ModelState.AddModelError(nameof(request.Email), "Disposable or temporary email addresses are not permitted. Please use a permanent email.");
                request.Step = 1;
                request.FormTimestampToken = _antiBotService.GenerateTimeLockToken();
                return View(request);
            }

            if (!await _antiBotService.HasValidDnsMxRecordAsync(request.Email))
            {
                ModelState.AddModelError(nameof(request.Email), "The domain for this email address does not exist or cannot receive mail.");
                request.Step = 1;
                request.FormTimestampToken = _antiBotService.GenerateTimeLockToken();
                return View(request);
            }

            // ── Step 5: Duplicate Account Check (Anti-Enumeration Defense) ────
            var cleanEmail = request.Email.Trim().ToLowerInvariant();
            var cleanPhone = (request.PhoneNumber ?? string.Empty).Trim();

            var existingUserByEmail = await _userManager.FindByEmailAsync(cleanEmail);
            var existingUserByPhone = !string.IsNullOrEmpty(cleanPhone) && await _userManager.Users.AnyAsync(u => u.PhoneNumber == cleanPhone);

            if (existingUserByEmail != null || existingUserByPhone)
            {
                // Anti-Enumeration: Do not confirm whether the account exists to the caller.
                // If the email is registered, notify the real owner via email in background.
                if (existingUserByEmail != null)
                {
                    var appName = client.DisplayName ?? tenant?.Name ?? _defaultSetting.ClientName;
                    var securityAlertMail = new MailRequest
                    {
                        To = new List<string> { cleanEmail },
                        Subject = $"Security Alert: Attempted registration on {appName}",
                        Body = $"Hello,<br/><br/>Someone recently attempted to register a new account on <b>{appName}</b> using your email address.<br/><br/>If this was you, you already have an active account! You can proceed to sign in with your existing credentials or reset your password if needed.<br/><br/>If this wasn't you, your account remains completely safe and no action is required."
                    };
                    _ = BackgroundJob.Enqueue(() => _mailService.SendAsync(securityAlertMail));
                }

                ModelState.AddModelError(string.Empty, "Unable to proceed with registration using this information. If you already have an account, please sign in.");
                request.Step = 1;
                request.FormTimestampToken = _antiBotService.GenerateTimeLockToken();
                return View(request);
            }

            // ── Model Validation ─────────────────────────────────────────────
            if (!ModelState.IsValid)
            {
                request.Step = 1;
                request.FormTimestampToken = _antiBotService.GenerateTimeLockToken();
                return View(request);
            }

            // ── Generate 6-Digit Email OTP ───────────────────────────────────
            var otpCode = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString("D6");
            var registrationToken = Guid.NewGuid().ToString("N");

            var pending = new PendingRegistrationModel
            {
                Token          = registrationToken,
                Name           = request.Name.Trim(),
                Email          = cleanEmail,
                PhoneNumber    = cleanPhone,
                Password       = string.Empty,
                ClientId       = clientId,
                TenantId       = request.TenantId,
                ReturnUrl      = request.ReturnUrl,
                OtpHash        = ComputeSha256(otpCode),
                OtpExpiryUtc   = DateTime.UtcNow.AddMinutes(10),
                FailedAttempts = 0,
                LastResendUtc  = DateTime.UtcNow,
                IsOtpVerified  = false
            };

            // Store in transient memory cache for 10 minutes (Zero DB pollution before OTP confirmation)
            _memoryCache.Set($"reg_pending_{registrationToken}", pending, TimeSpan.FromMinutes(10));

            // ── Dispatch Email OTP ───────────────────────────────────────────
            var tokens = new Dictionary<string, string>
            {
                ["UserName"]   = pending.Name,
                ["Email"]      = pending.Email,
                ["OtpCode"]    = otpCode,
                ["ExpiryMins"] = "10",
                ["AppName"]    = client.DisplayName ?? tenant?.Name ?? _defaultSetting.ClientName,
                ["TenantName"] = tenant?.Name ?? client.DisplayName ?? _defaultSetting.TenantName,
                ["LogoUrl"]    = tenant?.LogoUrl ?? client.LogoUrl ?? string.Empty
            };

            var emailTemplate = await _emailTemplateService.RenderAsync(EmailTriggerEvent.EmailConfirmation, tokens);
            var confirmMail = new MailRequest
            {
                To      = new List<string> { pending.Email },
                Subject = !string.IsNullOrWhiteSpace(emailTemplate?.Subject)
                    ? emailTemplate.Value.Subject
                    : $"Your verification code — {otpCode}",
                Body    = !string.IsNullOrWhiteSpace(emailTemplate?.Body)
                    ? emailTemplate.Value.Body
                    : $"Hello {pending.Name},<br/><br/>Your registration verification code is: <b style='font-size:24px;letter-spacing:4px;'>{otpCode}</b><br/><br/>This code will expire in 10 minutes."
            };
            _ = BackgroundJob.Enqueue(() => _mailService.SendAsync(confirmMail));

            // Transition View to Step 2
            request.Step = 2;
            request.RegistrationToken = registrationToken;
            ViewBag.MaskedEmail = MaskEmail(pending.Email);

            return View(request);
        }

        // ── Step 2: Verify Email OTP ─────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyRegistrationOtp(RegisterRequest request)
        {
            var clientId = ResolveClientId(request.ReturnUrl) ?? request.ClientId;
            var tenantId = ResolveTenantId(request.ReturnUrl) ?? request.TenantId;
            var tenant   = await _accessControlService.GetTenantByClientIdAsync(clientId, tenantId);
            var client   = await _applicationManager.FindByClientIdAsync(clientId) as ApplicationClient;

            ViewBag.TenantBranding = tenant;
            ViewBag.ClientName = client?.DisplayName;
            request.Step = 2;

            // Clear validation errors for fields not part of Step 2
            ModelState.Remove(nameof(request.Name));
            ModelState.Remove(nameof(request.Email));
            ModelState.Remove(nameof(request.PhoneNumber));
            ModelState.Remove(nameof(request.Password));
            ModelState.Remove(nameof(request.ConfirmPassword));

            if (string.IsNullOrWhiteSpace(request.RegistrationToken) ||
                !_memoryCache.TryGetValue($"reg_pending_{request.RegistrationToken}", out PendingRegistrationModel? pending) ||
                pending == null)
            {
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, "Your registration session has expired. Please fill in your details again.");
                request.Step = 1;
                request.FormTimestampToken = _antiBotService.GenerateTimeLockToken();
                return View("Register", request);
            }

            // Restore cached fields so model is complete
            request.Name = pending.Name;
            request.Email = pending.Email;
            request.PhoneNumber = pending.PhoneNumber;

            ViewBag.MaskedEmail = MaskEmail(pending.Email);

            if (string.IsNullOrWhiteSpace(request.VerificationCode))
            {
                ModelState.AddModelError(nameof(request.VerificationCode), "Please enter the 6-digit verification code.");
                return View("Register", request);
            }

            // Expiry check
            if (DateTime.UtcNow > pending.OtpExpiryUtc)
            {
                _memoryCache.Remove($"reg_pending_{request.RegistrationToken}");
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, "Verification code has expired. Please register again.");
                request.Step = 1;
                request.FormTimestampToken = _antiBotService.GenerateTimeLockToken();
                return View("Register", request);
            }

            // Attempt throttling check (max 5)
            if (pending.FailedAttempts >= 5)
            {
                _memoryCache.Remove($"reg_pending_{request.RegistrationToken}");
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, "Too many failed attempts. Registration session has been invalidated.");
                request.Step = 1;
                request.FormTimestampToken = _antiBotService.GenerateTimeLockToken();
                return View("Register", request);
            }

            // Constant-time hash check
            var submittedHash = ComputeSha256(request.VerificationCode.Trim());
            var isMatch = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(pending.OtpHash),
                Encoding.UTF8.GetBytes(submittedHash));

            if (!isMatch)
            {
                pending.FailedAttempts++;
                _memoryCache.Set($"reg_pending_{request.RegistrationToken}", pending, pending.OtpExpiryUtc - DateTime.UtcNow);
                var remaining = 5 - pending.FailedAttempts;
                ModelState.AddModelError(nameof(request.VerificationCode), $"Invalid verification code. {remaining} attempt(s) remaining.");
                return View("Register", request);
            }

            // ── Verified! Advance to Step 3 (Set Password) ───────────────────
            pending.IsOtpVerified = true;
            _memoryCache.Set($"reg_pending_{request.RegistrationToken}", pending, pending.OtpExpiryUtc - DateTime.UtcNow);

            ModelState.Clear(); // Clean state for Step 3
            request.Step = 3;
            request.VerificationCode = null;
            return View("Register", request);
        }

        // ── Step 3: Complete Registration (Set Password & Commit) ────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteRegistration(RegisterRequest request)
        {
            var clientId = ResolveClientId(request.ReturnUrl) ?? request.ClientId;
            var tenantId = ResolveTenantId(request.ReturnUrl) ?? request.TenantId;
            var tenant   = await _accessControlService.GetTenantByClientIdAsync(clientId, tenantId);
            var client   = await _applicationManager.FindByClientIdAsync(clientId) as ApplicationClient;

            ViewBag.TenantBranding = tenant;
            ViewBag.ClientName = client?.DisplayName;
            request.Step = 3;

            // Clear validation errors for fields not part of Step 3
            ModelState.Remove(nameof(request.Name));
            ModelState.Remove(nameof(request.Email));
            ModelState.Remove(nameof(request.PhoneNumber));
            ModelState.Remove(nameof(request.VerificationCode));

            if (string.IsNullOrWhiteSpace(request.RegistrationToken) ||
                !_memoryCache.TryGetValue($"reg_pending_{request.RegistrationToken}", out PendingRegistrationModel? pending) ||
                pending == null)
            {
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, "Your registration session has expired. Please start again.");
                request.Step = 1;
                request.FormTimestampToken = _antiBotService.GenerateTimeLockToken();
                return View("Register", request);
            }

            // Restore cached fields
            request.Name = pending.Name;
            request.Email = pending.Email;
            request.PhoneNumber = pending.PhoneNumber;

            if (!pending.IsOtpVerified)
            {
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, "Please verify your email address before creating a password.");
                request.Step = 2;
                ViewBag.MaskedEmail = MaskEmail(pending.Email);
                return View("Register", request);
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                ModelState.AddModelError(nameof(request.Password), "Password is required.");
                return View("Register", request);
            }

            if (request.Password.Length < 6)
            {
                ModelState.AddModelError(nameof(request.Password), "Password must be at least 6 characters long.");
                return View("Register", request);
            }

            if (request.Password != request.ConfirmPassword)
            {
                ModelState.AddModelError(nameof(request.ConfirmPassword), "Passwords do not match.");
                return View("Register", request);
            }

            // ── Create user account in AspNetUsers ───────────────────────────
            var user = new ApplicationUser
            {
                UserName             = pending.Email,
                Email                = pending.Email,
                PhoneNumber          = pending.PhoneNumber,
                Name                 = pending.Name,
                IsActive             = true,
                EmailConfirmed       = true,
                PhoneNumberConfirmed = false,
                TenantId             = pending.TenantId
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                foreach (var err in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                return View("Register", request);
            }

            // ── Dynamic Default Role per Client ──────────────────────────────
            var targetRole = !string.IsNullOrWhiteSpace(client?.DefaultRoleName)
                ? client.DefaultRoleName.Trim()
                : "End User";

            await _roleService.AddUserRoles(user, new List<string> { targetRole });

            // ── Permanent OpenIddict Client Authorization (Consent) ──────────
            if (client != null)
            {
                await _authorizationManager.CreateAsync(new OpenIddictAuthorizationDescriptor
                {
                    Subject       = user.Id.ToString(),
                    ApplicationId = client.Id.ToString(),
                    Status        = OpenIddictConstants.Statuses.Valid,
                    Type          = OpenIddictConstants.AuthorizationTypes.Permanent
                });
            }

            // ── Send Registration Welcome Email ──────────────────────────────
            var welcomeTokens = new Dictionary<string, string>
            {
                ["UserName"]   = user.Name ?? user.UserName ?? "User",
                ["Email"]      = user.Email ?? string.Empty,
                ["AppName"]    = client?.DisplayName ?? tenant?.Name ?? _defaultSetting.ClientName,
                ["TenantName"] = tenant?.Name ?? client?.DisplayName ?? _defaultSetting.TenantName,
                ["LogoUrl"]    = tenant?.LogoUrl ?? client?.LogoUrl ?? string.Empty
            };

            var regTemplate = await _emailTemplateService.RenderAsync(EmailTriggerEvent.Registration, welcomeTokens);
            var welcomeMailRequest = new MailRequest
            {
                To      = new List<string> { user.Email! },
                Subject = !string.IsNullOrWhiteSpace(regTemplate?.Subject)
                    ? regTemplate.Value.Subject
                    : $"Welcome to {client?.DisplayName ?? tenant?.Name ?? _defaultSetting.ClientName}",
                Body    = !string.IsNullOrWhiteSpace(regTemplate?.Body)
                    ? regTemplate.Value.Body
                    : $"Thank you for registering at {client?.DisplayName ?? tenant?.Name ?? _defaultSetting.ClientName}!"
            };
            _ = BackgroundJob.Enqueue(() => _mailService.SendAsync(welcomeMailRequest));

            // Clean up memory cache
            _memoryCache.Remove($"reg_pending_{request.RegistrationToken}");

            var appDisplayName = client?.DisplayName ?? tenant?.Name ?? _defaultSetting.ClientName;
            TempData["SuccessMessage"] = $"Your account has been created successfully! Welcome to {appDisplayName}. Please sign in to continue.";
            TempData["RegisteredEmail"] = user.Email;

            return RedirectToAction(nameof(Index), new { returnUrl = pending.ReturnUrl });
        }

        // ── Back Button Handlers ─────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BackToStep1(RegisterRequest request)
        {
            ModelState.Clear();
            var clientId = ResolveClientId(request.ReturnUrl) ?? request.ClientId;
            var tenantId = ResolveTenantId(request.ReturnUrl) ?? request.TenantId;
            var tenant   = await _accessControlService.GetTenantByClientIdAsync(clientId, tenantId);
            var client   = await _applicationManager.FindByClientIdAsync(clientId) as ApplicationClient;

            ViewBag.TenantBranding = tenant;
            ViewBag.ClientName = client?.DisplayName;

            if (!string.IsNullOrWhiteSpace(request.RegistrationToken) &&
                _memoryCache.TryGetValue($"reg_pending_{request.RegistrationToken}", out PendingRegistrationModel? pending) &&
                pending != null)
            {
                request.Name = pending.Name;
                request.Email = pending.Email;
                request.PhoneNumber = pending.PhoneNumber;
            }

            request.Step = 1;
            request.FormTimestampToken = _antiBotService.GenerateTimeLockToken();
            return View("Register", request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BackToStep2(RegisterRequest request)
        {
            ModelState.Clear();
            var clientId = ResolveClientId(request.ReturnUrl) ?? request.ClientId;
            var tenantId = ResolveTenantId(request.ReturnUrl) ?? request.TenantId;
            var tenant   = await _accessControlService.GetTenantByClientIdAsync(clientId, tenantId);
            var client   = await _applicationManager.FindByClientIdAsync(clientId) as ApplicationClient;

            ViewBag.TenantBranding = tenant;
            ViewBag.ClientName = client?.DisplayName;

            if (!string.IsNullOrWhiteSpace(request.RegistrationToken) &&
                _memoryCache.TryGetValue($"reg_pending_{request.RegistrationToken}", out PendingRegistrationModel? pending) &&
                pending != null)
            {
                ViewBag.MaskedEmail = MaskEmail(pending.Email);
            }

            request.Step = 2;
            return View("Register", request);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Terms of Service & Privacy Policy Pages
        // ─────────────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Terms(string? returnUrl = null)
        {
            var clientId = ResolveClientId(returnUrl);
            var tenantId = ResolveTenantId(returnUrl);
            var tenant   = await _accessControlService.GetTenantByClientIdAsync(clientId!, tenantId);
            var client   = !string.IsNullOrEmpty(clientId) ? await _applicationManager.FindByClientIdAsync(clientId) as ApplicationClient : null;

            ViewBag.TenantBranding = tenant;
            ViewBag.ClientName = client?.DisplayName;
            ViewBag.ReturnUrl = returnUrl;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Privacy(string? returnUrl = null)
        {
            var clientId = ResolveClientId(returnUrl);
            var tenantId = ResolveTenantId(returnUrl);
            var tenant   = await _accessControlService.GetTenantByClientIdAsync(clientId!, tenantId);
            var client   = !string.IsNullOrEmpty(clientId) ? await _applicationManager.FindByClientIdAsync(clientId) as ApplicationClient : null;

            ViewBag.TenantBranding = tenant;
            ViewBag.ClientName = client?.DisplayName;
            ViewBag.ReturnUrl = returnUrl;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendRegistrationOtp([FromBody] ResendRegistrationOtpRequest payload)
        {
            if (payload == null || string.IsNullOrWhiteSpace(payload.RegistrationToken))
            {
                return Json(new { success = false, message = "Invalid request." });
            }

            if (!_memoryCache.TryGetValue($"reg_pending_{payload.RegistrationToken}", out PendingRegistrationModel? pending) ||
                pending == null)
            {
                return Json(new { success = false, message = "Registration session has expired. Please register again." });
            }

            // Cooldown check (60 seconds)
            var elapsed = DateTime.UtcNow - pending.LastResendUtc;
            if (elapsed.TotalSeconds < 60)
            {
                var remaining = 60 - (int)elapsed.TotalSeconds;
                return Json(new { success = false, message = $"Please wait {remaining} seconds before requesting a new code." });
            }

            // Generate fresh OTP
            var otpCode = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString("D6");
            pending.OtpHash = ComputeSha256(otpCode);
            pending.OtpExpiryUtc = DateTime.UtcNow.AddMinutes(10);
            pending.FailedAttempts = 0;
            pending.LastResendUtc = DateTime.UtcNow;

            _memoryCache.Set($"reg_pending_{payload.RegistrationToken}", pending, TimeSpan.FromMinutes(10));

            var client = await _applicationManager.FindByClientIdAsync(pending.ClientId) as ApplicationClient;
            var tokens = new Dictionary<string, string>
            {
                ["UserName"]   = pending.Name,
                ["Email"]      = pending.Email,
                ["OtpCode"]    = otpCode,
                ["ExpiryMins"] = "10",
                ["AppName"]    = client?.DisplayName ?? _defaultSetting.ClientName,
                ["TenantName"] = client?.DisplayName ?? _defaultSetting.TenantName,
                ["LogoUrl"]    = client?.LogoUrl ?? string.Empty
            };

            var emailTemplate = await _emailTemplateService.RenderAsync(EmailTriggerEvent.EmailConfirmation, tokens);
            var confirmMail = new MailRequest
            {
                To      = new List<string> { pending.Email },
                Subject = !string.IsNullOrWhiteSpace(emailTemplate?.Subject)
                    ? emailTemplate.Value.Subject
                    : $"Your verification code — {otpCode}",
                Body    = !string.IsNullOrWhiteSpace(emailTemplate?.Body)
                    ? emailTemplate.Value.Body
                    : $"Hello {pending.Name},<br/><br/>Your new verification code is: <b style='font-size:24px;letter-spacing:4px;'>{otpCode}</b><br/><br/>This code will expire in 10 minutes."
            };
            _ = BackgroundJob.Enqueue(() => _mailService.SendAsync(confirmMail));

            return Json(new { success = true, message = "A new verification code has been sent to your email address." });
        }

        // ─────────────────────────────────────────────────────────────────────────
        // ConfirmEmail
        // ─────────────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);

            if (result.Succeeded)
            {
                var tokens = new Dictionary<string, string>
                {
                    ["UserName"]   = user.Name ?? user.UserName ?? "User",
                    ["Email"]      = user.Email ?? string.Empty,
                    ["AppName"]    = _defaultSetting.ClientName,
                    ["TenantName"] = _defaultSetting.TenantName,
                    ["LogoUrl"]    = string.Empty
                };

                var emailTemplate = await _emailTemplateService.RenderAsync(EmailTriggerEvent.WelcomeEmail, tokens);

                var welcomeMailRequest = new MailRequest
                {
                    To      = new List<string> { user.Email! },
                    Subject = !string.IsNullOrWhiteSpace(emailTemplate?.Subject) ? emailTemplate.Value.Subject : "Welcome!",
                    Body    = !string.IsNullOrWhiteSpace(emailTemplate?.Body) ? emailTemplate.Value.Body : "Welcome to the platform!"
                };
                await _mailService.SendAsync(welcomeMailRequest);
            }

            if (result.Succeeded)
            {
                ViewData["Message"] = "Email Verification is successful. Thank you for confirming your email.";
                return View("EmailConfirmed");
            }

            ViewData["Message"] = "Error confirming your email. The verification link might be expired or invalid.";
            return View("EmailConfirmed");
        }

        // ─────────────────────────────────────────────────────────────────────────
        // ForgotPassword / ResetPassword (unchanged logic, preserved)
        // ─────────────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> ForgotPassword(string? returnUrl = null)
        {
            await SetTenantBrandingAsync(returnUrl);
            return View(new ForgotPasswordRequest { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    TempData["SuccessMessage"] = "A password reset link has been sent to your email. Please check your inbox and follow the instructions to change your password.";
                    return RedirectToAction(nameof(Index), new { returnUrl = request.ReturnUrl });
                }

                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Action("ResetPassword", "Login",
                    values: new { token = code, email = request.Email, returnUrl = request.ReturnUrl },
                    protocol: Request.Scheme);

                var branding = await _accessControlService.GetTenantByClientIdAsync(
                    ResolveClientId(request.ReturnUrl), ResolveTenantId(request.ReturnUrl));

                var tokens = new Dictionary<string, string>
                {
                    ["UserName"]    = user.Name ?? user.UserName ?? "User",
                    ["Email"]       = user.Email ?? string.Empty,
                    ["AppName"]     = branding?.Name ?? _defaultSetting.ClientName,
                    ["TenantName"]  = branding?.Name ?? _defaultSetting.TenantName,
                    ["CallbackUrl"] = HtmlEncoder.Default.Encode(callbackUrl!),
                    ["LogoUrl"]     = branding?.LogoUrl ?? string.Empty
                };

                var emailTemplate = await _emailTemplateService.RenderAsync(EmailTriggerEvent.PasswordReset, tokens);
                var resetMail = new MailRequest
                {
                    To      = new List<string> { user.Email! },
                    Subject = !string.IsNullOrWhiteSpace(emailTemplate?.Subject) ? emailTemplate.Value.Subject : "Reset your password",
                    Body    = !string.IsNullOrWhiteSpace(emailTemplate?.Body) ? emailTemplate.Value.Body : $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl!)}'>clicking here</a>."
                };
                await _mailService.SendAsync(resetMail);

                TempData["SuccessMessage"] = "If an account with that email exists, a password reset link has been sent.";
                return RedirectToAction(nameof(Index), new { returnUrl = request.ReturnUrl });
            }

            await SetTenantBrandingAsync(request.ReturnUrl);

            return View(request);
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token = null, string email = null, string returnUrl = null)
        {
            if (token == null || email == null)
            {
                return BadRequest("A token and email must be supplied for password reset.");
            }

            await SetTenantBrandingAsync(returnUrl);

            var model = new ResetPasswordRequest { Token = token, Email = email, ReturnUrl = returnUrl };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                await SetTenantBrandingAsync(request.ReturnUrl);
                return View(request);
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                TempData["SuccessMessage"] = "Your password has been reset successfully. Please log in.";
                return RedirectToAction(nameof(Index), new { returnUrl = request.ReturnUrl });
            }

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.Password);
            if (result.Succeeded)
            {
                var tokens = new Dictionary<string, string>
                {
                    ["UserName"]   = user.Name ?? user.UserName ?? "User",
                    ["Email"]      = user.Email ?? string.Empty,
                    ["AppName"]    = _defaultSetting.ClientName,
                    ["TenantName"] = _defaultSetting.TenantName,
                    ["LogoUrl"]    = string.Empty
                };

                var emailTemplate = await _emailTemplateService.RenderAsync(EmailTriggerEvent.SecurityAlert, tokens);

                var passwordMailRequest = new MailRequest
                {
                    To      = new List<string> { user.Email! },
                    Subject = !string.IsNullOrWhiteSpace(emailTemplate?.Subject) ? emailTemplate.Value.Subject : "Security Alert: Password Reset Successful",
                    Body    = !string.IsNullOrWhiteSpace(emailTemplate?.Body) ? emailTemplate.Value.Body : "Your password has been successfully reset. If you did not perform this change, please contact support immediately."
                };
                _ = BackgroundJob.Enqueue(() => _mailService.SendAsync(passwordMailRequest));

                // Auto-login the user
                await _signInManager.SignInAsync(user, isPersistent: false);
                TempData["SuccessMessage"] = "Your password has been reset successfully.";

                if (!string.IsNullOrEmpty(request.ReturnUrl) && IsUrlLocalOrSameHost(request.ReturnUrl))
                {
                    return Redirect(request.ReturnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await SetTenantBrandingAsync(request.ReturnUrl);
            return View(request);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // CheckUserExists
        // ─────────────────────────────────────────────────────────────────────────

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> CheckUserExists(string? identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return Json(new { exists = false, message = "Email or username is required." });
            }

            var user = await FindUserByIdentifierAsync(identifier);
            return Json(new { exists = user != null });
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────────────

        private string ResolveClientId(string? returnUrl)
        {
            var values = ParseReturnUrlQuery(returnUrl);
            var clientId = values.TryGetValue("client_id", out var clientValues)
                ? clientValues.FirstOrDefault()
                : null;

            return string.IsNullOrWhiteSpace(clientId) ? _defaultSetting.ClientId : clientId;
        }

        private Guid? ResolveTenantId(string? returnUrl)
        {
            var values = ParseReturnUrlQuery(returnUrl);
            if (values.TryGetValue("tenant_id", out var tenantValues) &&
                Guid.TryParse(tenantValues.FirstOrDefault(), out var tenantId))
            {
                return tenantId;
            }

            return null;
        }

        private Dictionary<string, Microsoft.Extensions.Primitives.StringValues> ParseReturnUrlQuery(string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(StringComparer.OrdinalIgnoreCase);
            }

            var parts = returnUrl.Split('?', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                return new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(StringComparer.OrdinalIgnoreCase);
            }

            return QueryHelpers.ParseQuery(parts[1])
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
        }

        private async Task SetTenantBrandingAsync(string? returnUrl)
        {
            var clientId = ResolveClientId(returnUrl);
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                var client = await _applicationManager.FindByClientIdAsync(clientId) as ApplicationClient;
                ViewBag.ClientName = client?.DisplayName;
            }

            ViewBag.TenantBranding = await _accessControlService.GetTenantByClientIdAsync(
                clientId, ResolveTenantId(returnUrl));
        }

        private async Task SetLoginMethodViewBagAsync(string? returnUrl)
        {
            var client = await _applicationManager.FindByClientIdAsync(ResolveClientId(returnUrl)) as ApplicationClient;
            SetLoginMethodViewBag(client?.AllowedLoginMethod ?? LoginMethod.CredentialsOnly);
            ViewBag.AllowRegistration = client?.AllowPublicRegistration ?? false;
        }

        private void SetLoginMethodViewBag(LoginMethod method)
        {
            ViewBag.LoginMethod        = method.ToString();
            ViewBag.ShowMethodChooser  = method == LoginMethod.Both;
            ViewBag.IsMobileOtpOnly    = method == LoginMethod.MobileOtpOnly;
            ViewBag.IsCredentialsOnly  = method == LoginMethod.CredentialsOnly;
        }

        private async Task<ApplicationUser?> FindUserByIdentifierAsync(string? identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier)) return null;
            var norm = identifier.Trim();
            var user = await _userManager.FindByNameAsync(norm);
            if (user != null) return user;
            return norm.Contains('@') ? await _userManager.FindByEmailAsync(norm) : null;
        }

        private async Task<ApplicationUser?> FindUserByPhoneOrEmailAsync(string? identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier)) return null;
            var norm = identifier.Trim();

            // Try email first
            if (norm.Contains('@'))
                return await _userManager.FindByEmailAsync(norm);

            // Try phone number
            return await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == norm);
        }

        private async Task SendLockoutEmailAsync(ApplicationUser user, Domain.Entities.Tenants? branding)
        {
            var tokens = new Dictionary<string, string>
            {
                ["UserName"]   = user.Name ?? user.UserName ?? "User",
                ["Email"]      = user.Email ?? string.Empty,
                ["AppName"]    = branding?.Name ?? _defaultSetting.ClientName,
                ["TenantName"] = branding?.Name ?? _defaultSetting.TenantName,
                ["LogoUrl"]    = branding?.LogoUrl ?? string.Empty
            };
            var emailTemplate = await _emailTemplateService.RenderAsync(EmailTriggerEvent.AccountLocked, tokens);
            var lockMail = new MailRequest
            {
                To      = new List<string> { user.Email! },
                Subject = !string.IsNullOrWhiteSpace(emailTemplate?.Subject) ? emailTemplate.Value.Subject : "Account Locked",
                Body    = !string.IsNullOrWhiteSpace(emailTemplate?.Body) ? emailTemplate.Value.Body : "Your account has been temporarily locked for 30 minutes."
            };
            _ = BackgroundJob.Enqueue(() => _mailService.SendAsync(lockMail));
        }

        private async Task<string> BuildSignInErrorMessageAsync(ApplicationUser user, Microsoft.AspNetCore.Identity.SignInResult signInResult)
        {
            if (signInResult.IsLockedOut)
            {
                return "Account temporarily locked for 30 minutes. Contact admin or reset password to unlock.";
            }

            if (signInResult.IsNotAllowed && !await _userManager.IsEmailConfirmedAsync(user))
            {
                return "Confirm email before sign-in.";
            }

            if (signInResult.IsNotAllowed)
            {
                return "Access not allowed.";
            }

            return "Enter Correct Password.";
        }

        private bool IsUrlLocalOrSameHost(string? url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            if (Url.IsLocalUrl(url)) return true;

            try
            {
                var uri = new Uri(url);
                return uri.Host.Equals(Request.Host.Host, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string ComputeSha256(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes);
        }

        private static string MaskEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return string.Empty;
            var parts = email.Split('@');
            if (parts.Length != 2) return email;
            var name = parts[0];
            var maskedName = name.Length <= 2 ? name + "***" : name[0] + new string('*', Math.Max(2, name.Length - 2)) + name[^1];
            return $"{maskedName}@{parts[1]}";
        }
    }
}
