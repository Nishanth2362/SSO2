using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using SSO.Application.Interfaces.Services;
using SSO.Common.Constants.Permission;
using SSO.Domain.Entities;
using SSO.Infrastructure.Contexts;
using System.Collections.Immutable;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static SSO.Common.Constants.Application.ApplicationConstants;

namespace SSO.WebApplication.Controllers
{
    [AllowAnonymous]
    public class AuthorizationController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly IOpenIddictScopeManager _scopeManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly IAccessControlService _accessControlService;
        private readonly ITokenLifetimeSettingsService _tokenSettingsService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthorizationController> _logger;

        public AuthorizationController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IOpenIddictApplicationManager applicationManager,
            IOpenIddictScopeManager scopeManager,
            ApplicationDbContext dbContext,
            IConfiguration configuration,
            IAccessControlService accessControlService,
            ITokenLifetimeSettingsService tokenSettingsService,
            ILogger<AuthorizationController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _applicationManager = applicationManager;
            _scopeManager = scopeManager;
            _dbContext = dbContext;
            _configuration = configuration;
            _accessControlService = accessControlService;
            _tokenSettingsService = tokenSettingsService;
            _logger = logger;
        }

        #region Authorization Endpoint
        [HttpGet("~/connect/authorize")]
        [HttpPost("~/connect/authorize")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Authorize()
        
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            // Retrieve the user principal stored in the authentication cookie.
            var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);

            // If prompt=create or screen_hint=signup was requested, direct the user directly to the registration page
            var isSignUpRequested = HasPrompt(request, "create") ||
                                    string.Equals(Request.Query["screen_hint"], "signup", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(Request.Query["prompt"], "create", StringComparison.OrdinalIgnoreCase);

            if (isSignUpRequested)
            {
                var registerReturnUrl = Request.PathBase + Request.Path + QueryString.Create(
                    Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList());
                return RedirectToAction("Register", "Login", new { returnUrl = registerReturnUrl });
            }

            // If prompt=login was requested, force re-authentication even if user has active session
            if (HasPrompt(request, "login"))
            {
                return Challenge(
                    properties: new AuthenticationProperties
                    {
                        RedirectUri = Request.PathBase + Request.Path + QueryString.Create(
                            Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList())
                    },
                    authenticationSchemes: new[] { IdentityConstants.ApplicationScheme });
            }

            // If the user principal can't be extracted, redirect the user to the login page (or return login_required if prompt=none).
            if (!result.Succeeded)
            {
                if (HasPrompt(request, "none"))
                {
                    return Forbid(
                        properties: new AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.LoginRequired,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is not logged in."
                        }),
                        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }

                return Challenge(
                    properties: new AuthenticationProperties
                    {
                        RedirectUri = Request.PathBase + Request.Path + QueryString.Create(
                            Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList())
                    },
                    authenticationSchemes: new[] { IdentityConstants.ApplicationScheme });
            }

            var user = await _userManager.GetUserAsync(result.Principal) ??
                throw new InvalidOperationException("The user details cannot be retrieved.");

            // Create a new ClaimsPrincipal
            if (request.ClientId != null)
            {
                var error = await ValidateAccessAsync(user, request.ClientId);
                if (error != null)
                {
                    return Forbid(
                        properties: new AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.AccessDenied,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = error
                        }),
                        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }
            }

            var principal = await _signInManager.CreateUserPrincipalAsync(user);
            await EnrichPrincipalWithTenantClaimsAsync(principal, user);
            
            var scopes = request.GetScopes().ToList();
            principal.SetScopes(scopes);

            var resources = new HashSet<string>();

            foreach (var scope in scopes)
            {
                var scopeEntity = await _scopeManager.FindByNameAsync(scope);

                if (scopeEntity != null)
                {
                    var resArray = await _scopeManager.GetResourcesAsync(scopeEntity); // ✅ await

                    foreach (var res in resArray)
                    {
                        resources.Add(res);
                    }
                }
            }

            principal.SetResources(resources);
            //principal.SetAudiences("resource_server_api");
          

            var app = await _applicationManager.FindByClientIdAsync(request.ClientId!) as ApplicationClient;
            if (app != null)
            {
                if (!string.IsNullOrEmpty(app.Audience))
                {
                    principal.SetAudiences(app.Audience.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()));
                }
                
                var permissions = await _accessControlService.GetPermissionsForUserAsync(user.Id, app.Id);
                var identity = principal.Identities.First();

                // We used to clear existing permissions here, but the user requested "combined" permissions.
                // This allows SSO admin permissions (added by factory) to coexist with application-specific permissions.

                // Add correct permissions for this client
                foreach (var perm in permissions)
                {
                    if (!identity.HasClaim(ApplicationClaimTypes.Permission, perm))
                    {
                        identity.AddClaim(new Claim(ApplicationClaimTypes.Permission, perm));
                    }
                }
            }

            foreach (var claim in principal.Claims)
            {
                claim.SetDestinations(GetDestinations(claim, principal));
            }

            await ApplyDynamicTokenLifetimesAsync(principal, user?.TenantId);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        #endregion

        #region Token Endpoint
        [HttpPost("~/connect/token")]
        [IgnoreAntiforgeryToken]
        [Produces("application/json")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            if (request.IsPasswordGrantType())
            {
                var user = await _userManager.FindByNameAsync(request.Username!);
                if (user == null)
                {
                    return Forbid(
                        properties: new AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The username/password couple is invalid."
                        }),
                        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password!, lockoutOnFailure: true);
                if (!result.Succeeded)
                {
                    return Forbid(
                        properties: new AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The username/password couple is invalid."
                        }),
                        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }

                if (request.ClientId != null)
                {
                    var error = await ValidateAccessAsync(user, request.ClientId);
                    if (error != null)
                    {
                        return Forbid(
                            properties: new AuthenticationProperties(new Dictionary<string, string?>
                            {
                                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = error
                            }),
                            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                    }
                }

                var principal = await _signInManager.CreateUserPrincipalAsync(user);
                await EnrichPrincipalWithTenantClaimsAsync(principal, user);

                var scopes = request.GetScopes();
                principal.SetScopes(scopes);
                var resources = await _scopeManager.ListResourcesAsync(scopes).ToListAsync();
                principal.SetResources(resources);

                var app = await _applicationManager.FindByClientIdAsync(request.ClientId!) as ApplicationClient;
                if (app != null)
                {
                    if (!string.IsNullOrEmpty(app.Audience))
                    {
                        principal.SetAudiences(app.Audience.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()));
                    }

                    var permissions = await _accessControlService.GetPermissionsForUserAsync(user.Id, app.Id);
                    var identity = principal.Identities.First();

                    // We used to clear existing permissions here, but the user requested "combined" permissions.

                    // Add correct permissions for this client
                    foreach (var perm in permissions)
                    {
                        if (!identity.HasClaim(ApplicationClaimTypes.Permission, perm))
                        {
                            identity.AddClaim(new Claim(ApplicationClaimTypes.Permission, perm));
                        }
                    }
                }

                foreach (var claim in principal.Claims)
                {
                    claim.SetDestinations(GetDestinations(claim, principal));
                }

                await ApplyDynamicTokenLifetimesAsync(principal, user?.TenantId);

                return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
            else if (request.IsClientCredentialsGrantType())
            {
                // Note: the client credentials are automatically validated by OpenIddict:
                // if client_id or client_secret are invalid, this action won't be invoked.

                var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

                // Subject (sub) is a required claim, we use the client_id as the subject identifier.
                identity.AddClaim(Claims.Subject, request.ClientId ?? throw new InvalidOperationException());

                var principal = new ClaimsPrincipal(identity);

                principal.SetScopes(request.GetScopes());
                var resources = await _scopeManager.ListResourcesAsync(principal.GetScopes()).ToListAsync();
                principal.SetResources(resources);

                var app = await _applicationManager.FindByClientIdAsync(request.ClientId!) as ApplicationClient;
                if (app != null && !string.IsNullOrEmpty(app.Audience))
                {
                    principal.SetAudiences(app.Audience.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()));
                }

                foreach (var claim in principal.Claims)
                {
                    claim.SetDestinations(GetDestinations(claim, principal));
                }

                await ApplyDynamicTokenLifetimesAsync(principal, null);

                return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
            else if (request.IsAuthorizationCodeGrantType() || request.IsDeviceCodeGrantType() || request.IsRefreshTokenGrantType())
            {
                var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                var principal = result.Principal;

                if (principal == null)
                {
                     return Forbid(
                        properties: new AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is no longer valid."
                        }),
                        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }

                 // Retrieve the user profile corresponding to the authorization code/refresh token.
                var user = await _userManager.GetUserAsync(principal);
                if (user != null)
                {
                    await EnrichPrincipalWithTenantClaimsAsync(principal, user);

                    // Ensure the user is still allowed to sign in.
                    if (!await _signInManager.CanSignInAsync(user))
                    {
                        return Forbid(
                            properties: new AuthenticationProperties(new Dictionary<string, string?>
                            {
                                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is no longer allowed to sign in."
                            }),
                            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                    }
                }

                if (request.ClientId != null)
                {
                    var error = await ValidateAccessAsync(user, request.ClientId);
                    if (error != null)
                    {
                        return Forbid(
                            properties: new AuthenticationProperties(new Dictionary<string, string?>
                            {
                                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = error
                            }),
                            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                    }
                    
                    // REFRESH PERMISSIONS: Ensure the issued access token has the absolute latest permissions.
                    // This is critical for SSO systems where the user roles or client-specific permissions might change.
                    var app = await _applicationManager.FindByClientIdAsync(request.ClientId) as ApplicationClient;
                    if (app != null)
                    {
                        if (!string.IsNullOrEmpty(app.Audience))
                        {
                            principal.SetAudiences(app.Audience.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()));
                        }

                        if (user != null)
                        {
                            var permissions = await _accessControlService.GetPermissionsForUserAsync(user.Id, app.Id);
                            var identity = principal.Identities.First();

                            // Add correct permissions for this client (ensuring we don't duplicate existing ones in the subject)
                            foreach (var perm in permissions)
                            {
                                if (!identity.HasClaim(ApplicationClaimTypes.Permission, perm))
                                {
                                    identity.AddClaim(new Claim(ApplicationClaimTypes.Permission, perm));
                                }
                            }
                        }
                    }
                }

                foreach (var claim in principal.Claims)
                {
                    claim.SetDestinations(GetDestinations(claim, principal));
                }

                await ApplyDynamicTokenLifetimesAsync(principal, user?.TenantId);

                return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
            else if (request.IsTokenExchangeGrantType())
            {
                // Note: the delegation request is automatically validated by OpenIddict:
                // if client_id or subject_token are invalid, this action won't be invoked.

                var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                var principal = result.Principal;

                if (principal == null)
                {
                    return Forbid(
                        properties: new AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The subject token is invalid."
                        }),
                        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }

                foreach (var claim in principal.Claims)
                {
                    claim.SetDestinations(GetDestinations(claim, principal));
                }

                await ApplyDynamicTokenLifetimesAsync(principal, null);

                return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            throw new InvalidOperationException("The specified grant type is not supported.");
        }
        #endregion

        #region Introspection Endpoint
        [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
        [HttpPost("~/connect/introspect")]
        [IgnoreAntiforgeryToken]
        [Produces("application/json")]
        public async Task<IActionResult> Introspect()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            if (result.Principal is null)
            {
                return Forbid(
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is invalid."
                    }),
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            return SignIn(result.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        #endregion

        #region Revocation Endpoint
        [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
        [HttpPost("~/connect/revoke")]
        [IgnoreAntiforgeryToken]
        [Produces("application/json")]
        public async Task<IActionResult> Revoke()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            // OpenIddict handles the actual revocation logic. We just need to signal the result.
            return SignOut(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        #endregion

        #region Logout Endpoint
        [HttpGet("~/connect/endsession")]
        [HttpPost("~/connect/endsession")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);

                if (result.Succeeded)
                {
                    await _signInManager.SignOutAsync();
                }

                foreach (var cookieName in SSO.Common.Constants.Application.ApplicationConstants.RpapCookies.AllRpapCookies)
                {
                    Response.Cookies.Delete(cookieName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception during OpenID Connect endsession logout");
            }

            return SignOut(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = "/"
                });
        }
        #endregion

        #region UserInfo Endpoint
        [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
        [HttpGet("~/connect/userinfo")]
        [HttpPost("~/connect/userinfo")]
        [Produces("application/json")]
        public async Task<IActionResult> Userinfo()
         {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return Challenge(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The specified access token is bound to an account that no longer exists."
                    }));
            }

            var claims = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [Claims.Subject] = await _userManager.GetUserIdAsync(user)
            };

            if (User.HasScope(Scopes.Email))
            {
                claims[Claims.Email] = await _userManager.GetEmailAsync(user);
                claims[Claims.EmailVerified] = await _userManager.IsEmailConfirmedAsync(user);
            }

            if (User.HasScope(Scopes.Profile))
            {
                claims[Claims.Name] = user.Name; 
                claims[Claims.PreferredUsername] = user.UserName;
            }

            // Include user roles unconditionally in the UserInfo response
            var roles = await _userManager.GetRolesAsync(user);
            claims[Claims.Role] = roles.FirstOrDefault() ?? string.Empty;

            // Include mobile/phone number unconditionally if present
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (!string.IsNullOrEmpty(phoneNumber))
            {
                claims[Claims.PhoneNumber] = phoneNumber;
                claims[Claims.PhoneNumberVerified] = await _userManager.IsPhoneNumberConfirmedAsync(user);
            }


            // Include user ID and tenant ID
            var userIdStr = user.Id.ToString();
            var tenantIdStr = user.TenantId.ToString();
            claims["user_id"] = userIdStr;
            claims["userId"] = userIdStr;
            claims["tenant_id"] = tenantIdStr;
            claims["tenantId"] = tenantIdStr;
            claims["tenant"] = tenantIdStr;

            // Include permissions from the custom RolePermissions table
            var clientIdClaim = User.GetClaim(Claims.AuthorizedParty) ?? User.GetClaim(Claims.ClientId);
            if (Guid.TryParse(clientIdClaim, out var targetClientId))
            {
                var permissions = await _accessControlService.GetPermissionsForUserAsync(user.Id, targetClientId);
                if (permissions.Any())
                {
                    claims[ApplicationClaimTypes.Permission] = permissions;
                }
            }

            return Ok(claims);
        }
        #endregion

        #region Device Authorization & Verification
        // Device authorization endpoint handles starting a device flow login
        [HttpGet("~/connect/device")]
        [HttpPost("~/connect/device")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Device()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            return Forbid(
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidRequest,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The device authorization flow is currently handled natively or disabled."
                }),
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        [HttpGet("~/connect/verify")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Verify()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            // If the user principal can't be extracted, redirect the user to the login page.
            var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
            if (!result.Succeeded)
            {
                return Challenge(
                    properties: new AuthenticationProperties
                    {
                        RedirectUri = Request.PathBase + Request.Path + QueryString.Create(
                            Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList())
                    },
                    authenticationSchemes: new[] { IdentityConstants.ApplicationScheme });
            }

            // Retrieve the user profile corresponding to the authenticated principal.
            var user = await _userManager.GetUserAsync(result.Principal) ??
                throw new InvalidOperationException("The user details cannot be retrieved.");

            if (request.ClientId != null)
            {
                var error = await ValidateAccessAsync(user, request.ClientId);
                if (error != null)
                {
                    return Forbid(
                        properties: new AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.AccessDenied,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = error
                        }),
                        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }
            }

            var principal = await _signInManager.CreateUserPrincipalAsync(user);
            await EnrichPrincipalWithTenantClaimsAsync(principal, user);
            
            var scopes = request.GetScopes();
            principal.SetScopes(scopes);
            var resources = await _scopeManager.ListResourcesAsync(principal.GetScopes()).ToListAsync();
            principal.SetResources(resources);

            foreach (var claim in principal.Claims)
            {
                claim.SetDestinations(GetDestinations(claim, principal));
            }

            await ApplyDynamicTokenLifetimesAsync(principal, user?.TenantId);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        [HttpPost("~/connect/verify")]
        [IgnoreAntiforgeryToken]
        [Authorize(AuthenticationSchemes = "Identity.Application")]
        public async Task<IActionResult> VerifyAccept()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
               throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge(
                   properties: new AuthenticationProperties
                   {
                       RedirectUri = Request.PathBase + Request.Path + QueryString.Create(
                           Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList())
                   },
                   authenticationSchemes: new[] { IdentityConstants.ApplicationScheme });
            }

            if (request.ClientId != null)
            {
                var error = await ValidateAccessAsync(user, request.ClientId);
                if (error != null)
                {
                    return Forbid(
                        properties: new AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.AccessDenied,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = error
                        }),
                        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }
            }

            var principal = await _signInManager.CreateUserPrincipalAsync(user);
            await EnrichPrincipalWithTenantClaimsAsync(principal, user);
            
            var scopes = request.GetScopes();
            principal.SetScopes(scopes);
            var resources = await _scopeManager.ListResourcesAsync(principal.GetScopes()).ToListAsync();
            principal.SetResources(resources);

            foreach (var claim in principal.Claims)
            {
                claim.SetDestinations(GetDestinations(claim, principal));
            }

            await ApplyDynamicTokenLifetimesAsync(principal, user?.TenantId);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        #endregion

        private IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
        {
            // Note: by default, claims are NOT automatically included in the access and identity tokens.
            // Each claim must explicitly opt-in to one or both token destinations.
            switch (claim.Type)
            {
                case Claims.Name:
                case Claims.PreferredUsername:
                    yield return Destinations.AccessToken;
                    if (principal.HasScope(Scopes.Profile))
                        yield return Destinations.IdentityToken;
                    yield break;

                case Claims.Subject:
                    yield return Destinations.AccessToken;
                    yield return Destinations.IdentityToken;
                    yield break;

                case Claims.Email:
                case Claims.EmailVerified:
                    yield return Destinations.AccessToken;
                    if (principal.HasScope(Scopes.Email))
                        yield return Destinations.IdentityToken;
                    yield break;

                case Claims.Role:
                case ClaimTypes.Role:
                    yield return Destinations.AccessToken;
                    if (principal.HasScope("roles"))
                        yield return Destinations.IdentityToken;
                    yield break;

                // Custom permission claims → access token only (for API authorization)
                case ApplicationClaimTypes.Permission:
                    yield return Destinations.AccessToken;
                    yield break;

                case OpenIdDictCustomConstants.TenantId:
                case OpenIdDictCustomConstants.TenantName:
                case OpenIdDictCustomConstants.TenantCode:
                case OpenIdDictCustomConstants.TenantDatabaseProvider:
                case OpenIdDictCustomConstants.TenantDatabaseName:
                    yield return Destinations.AccessToken;
                    yield break;

                // Never include the security stamp in any token
                case "AspNet.Identity.SecurityStamp": 
                    yield break;

                default:
                    yield return Destinations.AccessToken;
                    yield break;
            }
        }

        private async Task<string?> ValidateAccessAsync(ApplicationUser user, string clientId)
        {
            return await _accessControlService.ValidateUserAccessAsync(user, clientId);
        }

        private async Task EnrichPrincipalWithTenantClaimsAsync(ClaimsPrincipal principal, ApplicationUser user)
        {
            var identity = principal.Identities.First();
            RemoveClaim(identity, Claims.Subject);
            RemoveClaim(identity, OpenIdDictCustomConstants.TenantId);
            RemoveClaim(identity, OpenIdDictCustomConstants.TenantName);
            RemoveClaim(identity, OpenIdDictCustomConstants.TenantCode);
            RemoveClaim(identity, OpenIdDictCustomConstants.TenantDatabaseProvider);
            RemoveClaim(identity, OpenIdDictCustomConstants.TenantDatabaseName);

            identity.AddClaim(new Claim(Claims.Subject, user.Id.ToString()));
            identity.AddClaim(new Claim(OpenIdDictCustomConstants.TenantId, user.TenantId.ToString()));

            var tenant = await _dbContext.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == user.TenantId);

            if (tenant == null)
            {
                return;
            }

            identity.AddClaim(new Claim(OpenIdDictCustomConstants.TenantName, tenant.Name));
            identity.AddClaim(new Claim(OpenIdDictCustomConstants.TenantCode, tenant.Code));

            if (!string.IsNullOrWhiteSpace(tenant.DatabaseProvider))
            {
                identity.AddClaim(new Claim(OpenIdDictCustomConstants.TenantDatabaseProvider, tenant.DatabaseProvider));
            }

            if (!string.IsNullOrWhiteSpace(tenant.DatabaseName))
            {
                identity.AddClaim(new Claim(OpenIdDictCustomConstants.TenantDatabaseName, tenant.DatabaseName));
            }
        }

        private static void RemoveClaim(ClaimsIdentity identity, string claimType)
        {
            foreach (var claim in identity.FindAll(claimType).ToList())
            {
                identity.RemoveClaim(claim);
            }
        }

        private async Task ApplyDynamicTokenLifetimesAsync(ClaimsPrincipal principal, Guid? tenantId = null)
        {
            try
            {
                var settings = await _tokenSettingsService.GetActiveSettingsAsync(tenantId);
                principal.SetAccessTokenLifetime(TimeSpan.FromMinutes(settings.AccessTokenLifetimeMinutes));
                principal.SetRefreshTokenLifetime(TimeSpan.FromDays(settings.RefreshTokenLifetimeDays));
                principal.SetAuthorizationCodeLifetime(TimeSpan.FromMinutes(settings.AuthorizationCodeLifetimeMinutes));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply dynamic token lifetimes to ClaimsPrincipal.");
            }
        }

        private static bool HasPrompt(OpenIddictRequest? request, string prompt)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Prompt)) return false;
            return request.Prompt.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Contains(prompt, StringComparer.OrdinalIgnoreCase);
        }
    }
}
