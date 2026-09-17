using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
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
using System.Text;
using System.Text.Encodings.Web;

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
        private readonly DefaultSetting _defaultSetting;
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly IOpenIddictAuthorizationManager _authorizationManager;
        private readonly IRoleService _roleService;
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
            IRoleService roleService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _accessControlService = accessControlService;
            _mailService = mailService;
            _emailTemplateService = emailTemplateService;
            _defaultSetting = configuration.GetSection("DefaultSetting").Get<DefaultSetting>()!;
            _applicationManager = applicationManager;
            _authorizationManager = authorizationManager;
            _roleService = roleService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? returnUrl = null)
        {
            await SetTenantBrandingAsync(returnUrl);

            ViewData["ReturnUrl"] = returnUrl;
            return View(new TokenRequest { ReturnUrl = returnUrl ?? string.Empty });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(TokenRequest request)
        {
            ViewData["ReturnUrl"] = request.ReturnUrl;
            request.Step = request.Step <= 1 ? 1 : 2;
            request.UserName = request.UserName?.Trim() ?? string.Empty;

            if (!ModelState.IsValid)
            {
                request.Step = string.IsNullOrWhiteSpace(request.UserName) ? 1 : 2;
            }
            else
            {
                var user = await FindUserByIdentifierAsync(request.UserName);
                if (user == null)
                {
                    ModelState.AddModelError(nameof(request.UserName), "We couldn't find an account with that email or username.");
                    request.ErrorMessage = "We couldn't find an account with that email or username.";
                    request.Step = 1;
                }
                else if (request.Step == 1)
                {
                    request.Step = 2;
                    request.Password = string.Empty;
                    request.ErrorMessage = string.Empty;
                    ModelState.Clear();
                }
                else
                {
                    bool wasLockedOut = await _userManager.IsLockedOutAsync(user);
                    var checkPasswordResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
                    if (checkPasswordResult.Succeeded)
                    {
                        var clientId = ResolveClientId(request.ReturnUrl);

                        var accessError = await _accessControlService.ValidateUserAccessAsync(user, clientId!);
                        if (accessError != null)
                        {
                            ModelState.AddModelError(string.Empty, accessError);
                            request.ErrorMessage = accessError;
                            request.Password = string.Empty;
                            request.Step = 2;
                            await SetTenantBrandingAsync(request.ReturnUrl);
                            return View(request);
                        }

                        await _signInManager.SignInAsync(user, request.RememberMe);

                        if (!string.IsNullOrEmpty(request.ReturnUrl) && Url.IsLocalUrl(request.ReturnUrl))
                        {
                            return Redirect(request.ReturnUrl);
                        }

                        return RedirectToAction("Index", "Home");
                    }

                    if (checkPasswordResult.IsLockedOut && !wasLockedOut)
                    {
                        var branding = await _accessControlService.GetTenantByClientIdAsync(
                            ResolveClientId(request.ReturnUrl), ResolveTenantId(request.ReturnUrl));

                        var tokens = new Dictionary<string, string>
                        {
                            ["UserName"]   = user.Name ?? user.UserName ?? "User",
                            ["Email"]      = user.Email ?? string.Empty,
                            ["AppName"]    = branding?.Name ?? _defaultSetting.ClientName,
                            ["TenantName"] = branding?.Name ?? _defaultSetting.TenantName,
                            ["LogoUrl"]    = branding?.LogoUrl ?? string.Empty
                        };

                        var emailTemplate = await _emailTemplateService.RenderAsync(EmailTriggerEvent.AccountLocked, tokens);

                        await _mailService.SendAsync(new MailRequest
                        {
                            To      = new List<string> { user.Email! },
                            Subject = emailTemplate?.Subject ?? "Account Locked",
                            Body    = emailTemplate?.Body    ?? "Your account has been locked due to too many failed login attempts."
                        });
                    }

                    request.ErrorMessage = await BuildSignInErrorMessageAsync(user, checkPasswordResult);
                    ModelState.AddModelError(string.Empty, request.ErrorMessage);
                    request.Step = 2;
                }
            }

            await SetTenantBrandingAsync(request.ReturnUrl);

            request.Password = string.Empty;
            return View(request);
        }
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Index));
        }

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

            var tenant = await _accessControlService.GetTenantByClientIdAsync(clientId!, tenantId);
            if (tenant == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid client application or tenant configuration. You cannot register.");
            }
            else if (!tenant.AllowPublicRegistration)
            {
                TempData["ErrorMessage"] = "Public registration is not available for this tenant. Please contact your administrator for an invitation.";
                return RedirectToAction(nameof(Index), new { returnUrl });
            }

            ViewBag.TenantBranding = tenant;

            return View(new RegisterRequest
            {
                ReturnUrl = returnUrl,
                ClientId = clientId,
                TenantId = tenant?.Id ?? Guid.Empty
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var tenant = await _accessControlService.GetTenantByClientIdAsync(request.ClientId, request.TenantId);
            if (tenant == null || tenant.Id != request.TenantId)
            {
                ModelState.AddModelError(string.Empty, "Invalid client application or tenant configuration.");
            }
            else if (!tenant.AllowPublicRegistration)
            {
                ModelState.AddModelError(string.Empty, "Public registration is not available for this tenant. Please contact your administrator for an invitation.");
            }

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    Name = request.Name,
                    IsActive = true,
                    TenantId = request.TenantId
                };

                var result = await _userManager.CreateAsync(user, request.Password);
                if (result.Succeeded)
                {
                    // ── 1. Assign the default "End User" role ─────────────────────
                    // _userManager.AddToRoleAsync(user, "End User");
                    RoleName = "End User";
                    await _roleService.AddUserRoles(user, new List<string> { RoleName });
                    // ── 2. Create permanent OpenIddict authorization (client consent) ──
                    // ApplicationId must be the DB Guid of the ApplicationClient entity,
                    // NOT the string client_id — same pattern as AddEditUserCommand.
                    var application = await _applicationManager.FindByClientIdAsync(request.ClientId) as ApplicationClient;
                    if (application != null)
                    {
                        await _authorizationManager.CreateAsync(new OpenIddictAuthorizationDescriptor
                        {
                            Subject = user.Id.ToString(),
                            ApplicationId = application.Id.ToString(),
                            Status = OpenIddictConstants.Statuses.Valid,
                            Type = OpenIddictConstants.AuthorizationTypes.Permanent
                        });
                    }

                    // ── 3. Send e-mail confirmation ───────────────────────────────
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Action(
                        "ConfirmEmail",
                        "Login",
                        values: new { userId = user.Id, code = code },
                        protocol: Request.Scheme);

                    // Try DB template first, fall back to a safe inline default
                    var tokens = new Dictionary<string, string>
                    {
                        ["UserName"]   = user.Name ?? user.UserName ?? "User",
                        ["Email"]      = user.Email ?? string.Empty,
                        ["AppName"]    = tenant?.Name ?? _defaultSetting.ClientName,
                        ["TenantName"] = tenant?.Name ?? _defaultSetting.TenantName,
                        ["CallbackUrl"] = HtmlEncoder.Default.Encode(callbackUrl!),
                        ["LogoUrl"]    = tenant?.LogoUrl ?? string.Empty
                    };

                    var emailTemplate = await _emailTemplateService.RenderAsync(
                        EmailTriggerEvent.EmailConfirmation, tokens);

                    await _mailService.SendAsync(new MailRequest
                    {
                        To      = new List<string> { user.Email! },
                        Subject = emailTemplate?.Subject ?? $"Confirm your email for {tenant?.Name ?? _defaultSetting.ClientName}",
                        Body    = emailTemplate?.Body    ?? $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl!)}'>clicking here</a>."
                    });

                    // Send Registration Welcome
                    var regTemplate = await _emailTemplateService.RenderAsync(
                        EmailTriggerEvent.Registration, tokens);

                    await _mailService.SendAsync(new MailRequest
                    {
                        To      = new List<string> { user.Email! },
                        Subject = regTemplate?.Subject ?? $"Welcome to {tenant?.Name ?? _defaultSetting.ClientName}",
                        Body    = regTemplate?.Body    ?? $"Thank you for registering at {tenant?.Name ?? _defaultSetting.ClientName}!"
                    });

                    TempData["SuccessMessage"] = "Registration successful. Please confirm your email before signing in.";
                    return RedirectToAction(nameof(Index), new { returnUrl = request.ReturnUrl });
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewBag.TenantBranding = tenant;

            return View(request);
        }

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

                await _mailService.SendAsync(new MailRequest
                {
                    To      = new List<string> { user.Email! },
                    Subject = emailTemplate?.Subject ?? "Welcome!",
                    Body    = emailTemplate?.Body    ?? "Welcome to the platform!"
                });
            }

            TempData["SuccessMessage"] = result.Succeeded ? "Thank you for confirming your email." : "Error confirming your email.";
            return RedirectToAction(nameof(Index));
        }

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
                    // Don't reveal that the user does not exist or is not confirmed
                    TempData["SuccessMessage"] = "If an account with that email exists, a password reset link has been sent.";
                    return RedirectToAction(nameof(Index), new { returnUrl = request.ReturnUrl });
                }

                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Action(
                    "ResetPassword",
                    "Login",
                    values: new { token = code, email = request.Email, returnUrl = request.ReturnUrl },
                    protocol: Request.Scheme);

                // Try DB template first, fall back to a safe inline default
                var branding = await _accessControlService.GetTenantByClientIdAsync(
                    ResolveClientId(request.ReturnUrl), ResolveTenantId(request.ReturnUrl));

                var tokens = new Dictionary<string, string>
                {
                    ["UserName"]   = user.Name ?? user.UserName ?? "User",
                    ["Email"]      = user.Email ?? string.Empty,
                    ["AppName"]    = branding?.Name ?? _defaultSetting.ClientName,
                    ["TenantName"] = branding?.Name ?? _defaultSetting.TenantName,
                    ["CallbackUrl"] = HtmlEncoder.Default.Encode(callbackUrl!),
                    ["LogoUrl"]    = branding?.LogoUrl ?? string.Empty
                };

                var emailTemplate = await _emailTemplateService.RenderAsync(
                    EmailTriggerEvent.PasswordReset, tokens);

                await _mailService.SendAsync(new MailRequest
                {
                    To      = new List<string> { user.Email! },
                    Subject = emailTemplate?.Subject ?? "Reset your password",
                    Body    = emailTemplate?.Body    ?? $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl!)}'>clicking here</a>."
                });

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
                TempData["SuccessMessage"] = "Your password has been reset successfully. Please log in.";
                return RedirectToAction(nameof(Index), new { returnUrl = request.ReturnUrl });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await SetTenantBrandingAsync(request.ReturnUrl);
            return View(request);
        }

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
            ViewBag.TenantBranding = await _accessControlService.GetTenantByClientIdAsync(ResolveClientId(returnUrl), ResolveTenantId(returnUrl));
        }

        private async Task<ApplicationUser?> FindUserByIdentifierAsync(string identifier)
        {
            var normalizedIdentifier = identifier.Trim();
            var user = await _userManager.FindByNameAsync(normalizedIdentifier);
            if (user != null)
            {
                return user;
            }

            return normalizedIdentifier.Contains('@')
                ? await _userManager.FindByEmailAsync(normalizedIdentifier)
                : null;
        }

        private async Task<string> BuildSignInErrorMessageAsync(ApplicationUser user, Microsoft.AspNetCore.Identity.SignInResult signInResult)
        {
            if (signInResult.IsLockedOut)
            {
                return "Your account is temporarily locked due to repeated failed sign-in attempts. Please try again later or reset your password.";
            }

            if (signInResult.IsNotAllowed && !await _userManager.IsEmailConfirmedAsync(user))
            {
                return "Please confirm your email address before signing in.";
            }

            if (signInResult.IsNotAllowed)
            {
                return "Your account is not allowed to sign in at the moment.";
            }

            return "Invalid login attempt.";
        }
    }
}
