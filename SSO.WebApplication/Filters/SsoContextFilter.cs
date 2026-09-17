using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using SSO.Application.Interfaces.Services;
using SSO.Common.Constants.Application;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SSO.WebApplication.Filters
{
    public class SsoContextFilter : IActionFilter
    {
        private const string ContextCookieName = "SSO_ViewContext";
        private const string ReturnUrlCookieName = "SSO_ReturnUrl";
        private const string TenantNameCookieName = "SSO_TenantName";

        public void OnActionExecuting(ActionExecutingContext context)
        {
            try
            {
                var httpContext = context.HttpContext;
                var request = httpContext.Request;

                var settingsService = httpContext.RequestServices.GetService(typeof(IRpapSettingsService)) as IRpapSettingsService;
                var cookieExpiryDays = settingsService?.GetSettings()?.ContextCookieExpiryDays ?? 7;
                if (cookieExpiryDays < 1) cookieExpiryDays = 7;

                // 1. Check for Active RPAP Protocol Session First
                string? rpapScope = request.Cookies[ApplicationConstants.RpapCookies.Scope];
                string? rpapTransactionId = request.Cookies[ApplicationConstants.RpapCookies.TransactionId];
                string? rpapCallbackUrl = request.Cookies[ApplicationConstants.RpapCookies.CallbackUrl];
                string? rpapTenantIdStr = request.Cookies[ApplicationConstants.RpapCookies.TenantId];

                // 2. Read query parameters or legacy cookies
                string? ssoMode = request.Query["sso_mode"];
                string? returnUrl = request.Query["returnUrl"];

                // If on login/authorize page, sso_mode might be nested inside the ReturnUrl parameter
                if (string.IsNullOrEmpty(ssoMode) && request.Query.TryGetValue("ReturnUrl", out var returnUrlVal))
                {
                    var nestedUrl = returnUrlVal.ToString();
                    if (!string.IsNullOrEmpty(nestedUrl))
                    {
                        try
                        {
                            var uri = new Uri("http://dummy" + (nestedUrl.StartsWith("/") ? nestedUrl : "/" + nestedUrl));
                            var query = QueryHelpers.ParseQuery(uri.Query);
                            if (query.TryGetValue("sso_mode", out var val))
                            {
                                ssoMode = val.ToString();
                            }
                            if (string.IsNullOrEmpty(returnUrl) && query.TryGetValue("returnUrl", out var rVal))
                            {
                                returnUrl = rVal.ToString();
                            }
                        }
                        catch
                        {
                            // Ignore parsing failures
                        }
                    }
                }

                // If RPAP session is active, set the activeMode to scoped restricted access
                if (!string.IsNullOrEmpty(rpapScope))
                {
                    ssoMode = "restricted_manage";
                    returnUrl = !string.IsNullOrEmpty(rpapCallbackUrl) ? rpapCallbackUrl : "/management/return";
                }

                string? activeMode = ssoMode;
                string? activeReturnUrl = returnUrl;

                // 3. Persist into Cookie if provided via Query
                if (!string.IsNullOrEmpty(ssoMode))
                {
                    httpContext.Response.Cookies.Append(ContextCookieName, ssoMode, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Lax,
                        Expires = DateTimeOffset.UtcNow.AddDays(cookieExpiryDays)
                    });
                }
                else
                {
                    activeMode = request.Cookies[ContextCookieName];
                }

                if (!string.IsNullOrEmpty(returnUrl))
                {
                    httpContext.Response.Cookies.Append(ReturnUrlCookieName, returnUrl, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Lax,
                        Expires = DateTimeOffset.UtcNow.AddDays(cookieExpiryDays)
                    });
                }
                else
                {
                    activeReturnUrl = request.Cookies[ReturnUrlCookieName];
                }

                // 4. Scope-based Dynamic URL & Action Enforcement for RPAP
                if (!string.IsNullOrEmpty(rpapScope))
                {
                    var actionName = context.RouteData.Values["action"]?.ToString() ?? "";
                    var controllerName = context.RouteData.Values["controller"]?.ToString() ?? "";

                    bool isAllowed = true;
                    if (settingsService != null)
                    {
                        isAllowed = settingsService.IsActionAllowed(rpapScope, controllerName, actionName);
                    }

                    if (!isAllowed)
                    {
                        var fallbackRedirect = settingsService?.GetFallbackUnauthorizedRedirectUrl() ?? "/Account/Profile";
                        context.Result = new RedirectResult(fallbackRedirect);
                        return;
                    }
                }

                // 5. Tenant Name resolution for UI Header
                string? tenantName = null;
                try
                {
                    var dbContext = httpContext.RequestServices.GetService(typeof(SSO.Infrastructure.Contexts.ApplicationDbContext))
                        as SSO.Infrastructure.Contexts.ApplicationDbContext;

                    if (dbContext != null)
                    {
                        Guid? tenantId = null;

                        if (!string.IsNullOrEmpty(rpapTenantIdStr) && Guid.TryParse(rpapTenantIdStr, out var rpapTenantGuid))
                        {
                            tenantId = rpapTenantGuid;
                        }
                        else
                        {
                            var currentUserService = httpContext.RequestServices.GetService(typeof(SSO.Application.Interfaces.Services.ICurrentUserService))
                                as SSO.Application.Interfaces.Services.ICurrentUserService;
                            if (currentUserService != null && !currentUserService.IsMasterTenant)
                            {
                                tenantId = currentUserService.TenantId;
                            }
                        }

                        if (tenantId == null)
                        {
                            string? tenantIdStr = request.Query["tenant_id"];
                            if (string.IsNullOrEmpty(tenantIdStr)) tenantIdStr = request.Query["tenantId"];

                            if (!string.IsNullOrEmpty(tenantIdStr) && Guid.TryParse(tenantIdStr, out var tid))
                            {
                                tenantId = tid;
                            }
                            else if (!string.IsNullOrEmpty(activeReturnUrl))
                            {
                                var match = dbContext.Clients
                                    .AsNoTracking()
                                    .Include(c => c.TenantClients)
                                    .FirstOrDefault(c => EF.Functions.Like(c.RedirectUris, $"%{activeReturnUrl}%"));
                                if (match != null)
                                {
                                    tenantId = match.TenantClients.FirstOrDefault()?.TenantId;
                                }
                            }
                        }

                        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
                        {
                            var tenant = dbContext.Tenants
                                .AsNoTracking()
                                .FirstOrDefault(t => t.Id == tenantId.Value);
                            tenantName = tenant?.Name;
                        }

                        if (!string.IsNullOrEmpty(tenantName))
                        {
                            httpContext.Response.Cookies.Append(TenantNameCookieName, tenantName, new CookieOptions
                            {
                                HttpOnly = true,
                                Secure = true,
                                SameSite = SameSiteMode.Lax,
                                Expires = DateTimeOffset.UtcNow.AddDays(cookieExpiryDays)
                            });
                        }
                        else
                        {
                            tenantName = request.Cookies[TenantNameCookieName];
                        }
                    }
                }
                catch
                {
                    // Safe fallback in case of db errors
                }

                if (context.Controller is Controller controller)
                {
                    controller.ViewData["SSO_ViewContext"] = activeMode;
                    controller.ViewData["SSO_ReturnUrl"] = !string.IsNullOrEmpty(rpapTransactionId) ? "/management/return" : activeReturnUrl;
                    controller.ViewData["SSO_TenantName"] = tenantName;
                    controller.ViewData["RPAP_Active"] = !string.IsNullOrEmpty(rpapScope);
                    controller.ViewData["RPAP_Scope"] = rpapScope;
                    controller.ViewData["RPAP_TransactionId"] = rpapTransactionId;
                }
            }
            catch
            {
                // Never allow context filter failures to crash application request
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No action needed after execution
        }
    }
}
