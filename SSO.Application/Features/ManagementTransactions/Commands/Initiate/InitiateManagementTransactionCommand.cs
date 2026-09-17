using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Interfaces.Services;
using SSO.Common.Constants.Application;
using SSO.Common.Wrapper;
using SSO.Domain.Entities;
using SSO.Domain.Enums;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.ManagementTransactions.Commands.Initiate
{
    public class InitiateManagementTransactionRequest
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }
        public string Scope { get; set; } = string.Empty;
        public string CallbackUrl { get; set; } = string.Empty;
        public string? State { get; set; }
        public string? TargetUrl { get; set; }
        public int? TtlSeconds { get; set; }
    }

    public class InitiateManagementTransactionResult
    {
        public Guid TransactionId { get; set; }
        public string LaunchToken { get; set; } = string.Empty;
        public string LaunchUrl { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public DateTime ExpiresOn { get; set; }
        public string Scope { get; set; } = string.Empty;
        public string TargetUrl { get; set; } = string.Empty;
    }

    public record InitiateManagementTransactionCommand : IRequest<Result<InitiateManagementTransactionResult>>
    {
        public string ClientId { get; init; } = string.Empty;
        public string ClientSecret { get; init; } = string.Empty;
        public Guid UserId { get; init; }
        public Guid TenantId { get; init; }
        public string Scope { get; init; } = string.Empty;
        public string CallbackUrl { get; init; } = string.Empty;
        public string? State { get; init; }
        public string? TargetUrl { get; init; }
        public int? TtlSeconds { get; init; }
        public string? BaseUrl { get; init; }
    }

    internal class InitiateManagementTransactionCommandHandler : IRequestHandler<InitiateManagementTransactionCommand, Result<InitiateManagementTransactionResult>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly IDateTimeService _dateTimeService;
        private readonly IRpapSettingsService _settingsService;

        public InitiateManagementTransactionCommandHandler(
            IUnitOfWork<Guid> unitOfWork,
            UserManager<ApplicationUser> userManager,
            IOpenIddictApplicationManager applicationManager,
            IDateTimeService dateTimeService,
            IRpapSettingsService settingsService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _applicationManager = applicationManager;
            _dateTimeService = dateTimeService;
            _settingsService = settingsService;
        }

        public async Task<Result<InitiateManagementTransactionResult>> Handle(InitiateManagementTransactionCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.ClientId))
                    return await Result<InitiateManagementTransactionResult>.FailAsync("ClientId is required.");

                if (string.IsNullOrWhiteSpace(request.ClientSecret))
                    return await Result<InitiateManagementTransactionResult>.FailAsync("ClientSecret is required.");

                if (string.IsNullOrWhiteSpace(request.Scope))
                    return await Result<InitiateManagementTransactionResult>.FailAsync("Scope is required (e.g. profile:manage, users:manage, users_roles:manage).");

                if (string.IsNullOrWhiteSpace(request.CallbackUrl))
                    return await Result<InitiateManagementTransactionResult>.FailAsync("CallbackUrl is required.");

                // 1. Validate Scope
                var normalizedScope = request.Scope.Trim().ToLowerInvariant();
                if (!_settingsService.IsScopeAllowed(normalizedScope))
                {
                    var allowed = _settingsService.GetSettings().AllowedScopes;
                    return await Result<InitiateManagementTransactionResult>.FailAsync($"Unsupported or disabled scope '{request.Scope}'. Allowed scopes: {string.Join(", ", allowed)}.");
                }

                // 2. Validate Client
                var app = await _applicationManager.FindByClientIdAsync(request.ClientId, cancellationToken);
                if (app == null)
                {
                    return await Result<InitiateManagementTransactionResult>.FailAsync("Invalid ClientId.");
                }

                var client = app as ApplicationClient;
                if (client == null || !client.IsActive)
                {
                    return await Result<InitiateManagementTransactionResult>.FailAsync("Invalid or inactive ClientId.");
                }

                // Authenticate client secret via OpenIddict
                if (!await _applicationManager.ValidateClientSecretAsync(app, request.ClientSecret, cancellationToken))
                {
                    return await Result<InitiateManagementTransactionResult>.FailAsync("Client authentication failed. Invalid ClientSecret.");
                }

                // 3. Validate Tenant
                var tenant = await _unitOfWork.Repository<Domain.Entities.Tenants>().Entities
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

                if (tenant == null || !tenant.IsActive)
                    return await Result<InitiateManagementTransactionResult>.FailAsync("Invalid or inactive TenantId.");

                // Verify client has access to this tenant (check TenantClients)
                var hasTenantAccess = await _unitOfWork.Repository<TenantClient>().Entities
                    .AsNoTracking()
                    .AnyAsync(tc => tc.TenantId == request.TenantId && tc.ApplicationClientId == client.Id, cancellationToken);

                if (!hasTenantAccess)
                    return await Result<InitiateManagementTransactionResult>.FailAsync("Client is not authorized for the specified tenant.");

                // 4. Validate User
                var user = await _userManager.FindByIdAsync(request.UserId.ToString());
                if (user == null || !user.IsActive)
                    return await Result<InitiateManagementTransactionResult>.FailAsync("Invalid or inactive UserId.");

                // User must belong to this tenant
                if (user.TenantId != request.TenantId)
                    return await Result<InitiateManagementTransactionResult>.FailAsync("User does not belong to the specified tenant.");

                // 5. Validate Callback URL
                if (!Uri.TryCreate(request.CallbackUrl, UriKind.Absolute, out var callbackUriObj) ||
                    (callbackUriObj.Scheme != Uri.UriSchemeHttp && callbackUriObj.Scheme != Uri.UriSchemeHttps))
                {
                    return await Result<InitiateManagementTransactionResult>.FailAsync($"Invalid CallbackUrl format: '{request.CallbackUrl}'. Must be an absolute HTTP or HTTPS URL.");
                }

                // Gather all registered redirect URIs for this client and any sibling client belonging to this tenant
                var uriCandidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                void ExtractUrisFromRaw(string? raw)
                {
                    if (string.IsNullOrWhiteSpace(raw)) return;
                    var cleaned = raw.Trim().Trim('[', ']');
                    var parts = cleaned.Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        var cleanPart = part.Trim().Trim('"', '\'').Trim();
                        if (!string.IsNullOrWhiteSpace(cleanPart))
                            uriCandidates.Add(cleanPart);
                    }
                }

                // Current client URIs
                var registeredRedirectUris = await _applicationManager.GetRedirectUrisAsync(app, cancellationToken);
                var registeredPostLogoutUris = await _applicationManager.GetPostLogoutRedirectUrisAsync(app, cancellationToken);

                foreach (var u in registeredRedirectUris)
                    if (!string.IsNullOrWhiteSpace(u)) uriCandidates.Add(u.Trim());

                foreach (var u in registeredPostLogoutUris)
                    if (!string.IsNullOrWhiteSpace(u)) uriCandidates.Add(u.Trim());

                ExtractUrisFromRaw(client.RedirectUris);
                ExtractUrisFromRaw(client.PostLogoutRedirectUris);
                if (!string.IsNullOrWhiteSpace(client.Website))
                    uriCandidates.Add(client.Website.Trim());

                // Check sibling clients belonging to this tenant (e.g. printa-spa-test)
                var tenantClientIds = await _unitOfWork.Repository<TenantClient>().Entities
                    .AsNoTracking()
                    .Where(tc => tc.TenantId == request.TenantId)
                    .Select(tc => tc.ApplicationClientId)
                    .ToListAsync(cancellationToken);

                foreach (var siblingClientId in tenantClientIds)
                {
                    var siblingApp = await _applicationManager.FindByIdAsync(siblingClientId.ToString(), cancellationToken);
                    if (siblingApp != null)
                    {
                        var siblingRedirects = await _applicationManager.GetRedirectUrisAsync(siblingApp, cancellationToken);
                        foreach (var u in siblingRedirects)
                            if (!string.IsNullOrWhiteSpace(u)) uriCandidates.Add(u.Trim());

                        if (siblingApp is ApplicationClient siblingClient)
                        {
                            ExtractUrisFromRaw(siblingClient.RedirectUris);
                            ExtractUrisFromRaw(siblingClient.PostLogoutRedirectUris);
                            if (!string.IsNullOrWhiteSpace(siblingClient.Website))
                                uriCandidates.Add(siblingClient.Website.Trim());
                        }
                    }
                }

                if (uriCandidates.Count > 0)
                {
                    var isRegistered = uriCandidates.Any(registeredUri =>
                    {
                        var cleanRegistered = registeredUri.Trim();

                        // Direct match or prefix match
                        if (request.CallbackUrl.StartsWith(cleanRegistered, StringComparison.OrdinalIgnoreCase) ||
                            cleanRegistered.StartsWith(request.CallbackUrl, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }

                        if (Uri.TryCreate(cleanRegistered, UriKind.Absolute, out var regUriObj))
                        {
                            // Exact origin match (e.g. http://localhost:3000 == http://localhost:3000)
                            if (string.Equals(callbackUriObj.GetLeftPart(UriPartial.Authority),
                                                 regUriObj.GetLeftPart(UriPartial.Authority),
                                                 StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }

                            // Localhost / Development loopback flexibility (e.g. localhost:3000 vs localhost:3003)
                            var isCallbackLocal = callbackUriObj.IsLoopback || callbackUriObj.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);
                            var isRegLocal = regUriObj.IsLoopback || regUriObj.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);
                            if (isCallbackLocal && isRegLocal)
                            {
                                return true;
                            }
                        }

                        return false;
                    });

                    if (!isRegistered)
                    {
                        return await Result<InitiateManagementTransactionResult>.FailAsync($"CallbackUrl '{request.CallbackUrl}' is not registered for this client or tenant. Registered: {string.Join(", ", uriCandidates)}");
                    }
                }

                // 6. Determine Target Landing URL
                var targetUrl = _settingsService.ResolveLandingUrl(normalizedScope, request.TargetUrl);

                // 7. Generate High-Entropy Launch Token & Hash
                var rawTokenBytes = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(rawTokenBytes);
                }
                var launchToken = Convert.ToHexString(rawTokenBytes).ToLowerInvariant();
                var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(launchToken))).ToLowerInvariant();

                // 8. Determine Expiry (respects frontend requested TTL clamped within configured bounds)
                var ttl = _settingsService.ResolveTtl(request.TtlSeconds, normalizedScope);
                var now = _dateTimeService.NowUtc;
                var expiresOn = now.AddSeconds(ttl);

                // 9. Save Transaction
                var transaction = new ManagementTransaction
                {
                    Id = Guid.NewGuid(),
                    ClientId = request.ClientId,
                    TenantId = request.TenantId,
                    UserId = request.UserId,
                    Scope = normalizedScope,
                    LaunchTokenHash = tokenHash,
                    TargetUrl = targetUrl,
                    CallbackUrl = request.CallbackUrl,
                    State = request.State,
                    ExpiresOn = expiresOn,
                    IsConsumed = false,
                    Status = ManagementTransactionStatus.Pending
                };

                await _unitOfWork.Repository<ManagementTransaction>().AddAsync(transaction);
                await _unitOfWork.Commit(cancellationToken);

                var baseUrl = (request.BaseUrl ?? string.Empty).TrimEnd('/');
                var launchUrl = $"{baseUrl}/management/launch?t={launchToken}";

                return await Result<InitiateManagementTransactionResult>.SuccessAsync(new InitiateManagementTransactionResult
                {
                    TransactionId = transaction.Id,
                    LaunchToken = launchToken,
                    LaunchUrl = launchUrl,
                    ExpiresIn = ttl,
                    ExpiresOn = expiresOn,
                    Scope = normalizedScope,
                    TargetUrl = targetUrl
                }, "Restricted Page Access transaction initiated successfully.");
            }
            catch (Exception ex)
            {
                return await Result<InitiateManagementTransactionResult>.FailAsync($"Error initiating management transaction: {ex.Message}");
            }
        }
    }
}
