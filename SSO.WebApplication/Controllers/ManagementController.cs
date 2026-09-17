using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SSO.Application.Features.ManagementTransactions.Commands.Complete;
using SSO.Application.Features.ManagementTransactions.Commands.Consume;
using SSO.Application.Features.ManagementTransactions.Commands.Initiate;
using SSO.Application.Features.ManagementTransactions.Commands.Verify;
using SSO.Common.Constants.Application;
using SSO.Domain.Entities;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SSO.WebApplication.Controllers
{
    public class ManagementController : Controller
    {
        private readonly IMediator _mediator;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ManagementController> _logger;

        public ManagementController(
            IMediator mediator,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<ManagementController> logger)
        {
            _mediator = mediator;
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        #region Server-to-Server Backchannel APIs

        /// <summary>
        /// Initiates a single-use Restricted Page Access transaction from an authenticated client backend.
        /// </summary>
        [HttpPost("~/api/v1/management/transactions/initiate")]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> InitiateTransaction([FromBody] InitiateManagementTransactionRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { succeeded = false, message = "Invalid request payload." });
                }

                var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

                var command = new InitiateManagementTransactionCommand
                {
                    ClientId = request.ClientId,
                    ClientSecret = request.ClientSecret,
                    UserId = request.UserId,
                    TenantId = request.TenantId,
                    Scope = request.Scope,
                    CallbackUrl = request.CallbackUrl,
                    State = request.State,
                    TargetUrl = request.TargetUrl,
                    TtlSeconds = request.TtlSeconds,
                    BaseUrl = baseUrl
                };

                var result = await _mediator.Send(command);
                if (result.Succeeded)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in InitiateTransaction");
                return StatusCode(StatusCodes.Status500InternalServerError, new { succeeded = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Verifies a one-time result code presented to the client application's callback URL.
        /// </summary>
        [HttpPost("~/api/v1/management/transactions/verify-result")]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> VerifyResult([FromBody] VerifyManagementResultRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { succeeded = false, message = "Invalid request payload." });
                }

                var command = new VerifyManagementResultCommand
                {
                    ClientId = request.ClientId,
                    ClientSecret = request.ClientSecret,
                    Code = request.Code
                };

                var result = await _mediator.Send(command);
                if (result.Succeeded)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in VerifyResult");
                return StatusCode(StatusCodes.Status500InternalServerError, new { succeeded = false, message = ex.Message });
            }
        }

        #endregion

        #region Browser Launch & Return Endpoints

        /// <summary>
        /// Browser entry point for single-use Restricted Page Access.
        /// Validates the token, signs in the target user, establishes the scoped ephemeral session, and lands on target page.
        /// </summary>
        [HttpGet("~/management/launch")]
        [AllowAnonymous]
        public async Task<IActionResult> Launch([FromQuery] string t)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(t))
                {
                    TempData["ErrorMessage"] = "Missing Restricted Page Access launch token.";
                    return RedirectToAction("Index", "Login");
                }

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = Request.Headers["User-Agent"].ToString();

                var consumeResult = await _mediator.Send(new ConsumeManagementLaunchCommand
                {
                    LaunchToken = t,
                    IpAddress = ipAddress,
                    UserAgent = userAgent
                });

                if (!consumeResult.Succeeded)
                {
                    TempData["ErrorMessage"] = consumeResult.Messages?.Count > 0 ? consumeResult.Messages[0] : "Invalid or expired launch token.";
                    return RedirectToAction("Index", "Login");
                }

                var data = consumeResult.Data;

                // Ensure the target user is signed in
                var targetUser = await _userManager.FindByIdAsync(data.UserId.ToString());
                if (targetUser == null || !targetUser.IsActive)
                {
                    TempData["ErrorMessage"] = "Target user account not found or inactive.";
                    return RedirectToAction("Index", "Login");
                }

                // Sign in the user if not currently signed in as this user
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (currentUserId != data.UserId.ToString())
                {
                    await _signInManager.SignInAsync(targetUser, isPersistent: false);
                }

                // Set Ephemeral Scoped Session Cookies (2 hours TTL max)
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddHours(2)
                };

                Response.Cookies.Append(ApplicationConstants.RpapCookies.TransactionId, data.TransactionId.ToString(), cookieOptions);
                Response.Cookies.Append(ApplicationConstants.RpapCookies.Scope, data.Scope, cookieOptions);
                Response.Cookies.Append(ApplicationConstants.RpapCookies.TenantId, data.TenantId.ToString(), cookieOptions);
                Response.Cookies.Append(ApplicationConstants.RpapCookies.ClientId, data.ClientId, cookieOptions);
                Response.Cookies.Append(ApplicationConstants.RpapCookies.CallbackUrl, data.CallbackUrl, cookieOptions);

                // Redirect directly to the requested restricted target page
                var targetUrl = !string.IsNullOrEmpty(data.TargetUrl) ? data.TargetUrl : "/Account/Profile";
                return Redirect(targetUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in Launch endpoint");
                TempData["ErrorMessage"] = "An error occurred while launching restricted page access.";
                return RedirectToAction("Index", "Login");
            }
        }

        /// <summary>
        /// Completes the restricted session, generates a one-time result code, and redirects browser back to registered client callback.
        /// </summary>
        [HttpGet("~/management/return")]
        [Authorize]
        public async Task<IActionResult> ReturnToApp()
        {
            try
            {
                var transactionIdStr = Request.Cookies[ApplicationConstants.RpapCookies.TransactionId];
                var callbackUrl = Request.Cookies[ApplicationConstants.RpapCookies.CallbackUrl];

                if (Guid.TryParse(transactionIdStr, out var transactionId))
                {
                    var completeResult = await _mediator.Send(new CompleteManagementTransactionCommand
                    {
                        TransactionId = transactionId
                    });

                    // Clear all session cookies
                    foreach (var cookieName in ApplicationConstants.RpapCookies.AllRpapCookies)
                    {
                        Response.Cookies.Delete(cookieName);
                    }

                    if (completeResult.Succeeded && !string.IsNullOrEmpty(completeResult.Data.RedirectUriWithCode))
                    {
                        return Redirect(completeResult.Data.RedirectUriWithCode);
                    }
                }

                if (!string.IsNullOrEmpty(callbackUrl))
                {
                    return Redirect(callbackUrl);
                }

                return RedirectToAction("Profile", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in ReturnToApp endpoint");

                // Clear cookies on error to prevent stuck state
                foreach (var cookieName in ApplicationConstants.RpapCookies.AllRpapCookies)
                {
                    try { Response.Cookies.Delete(cookieName); } catch { }
                }

                return RedirectToAction("Index", "Login");
            }
        }

        #endregion
    }
}
