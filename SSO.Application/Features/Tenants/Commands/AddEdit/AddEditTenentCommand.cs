using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Common.Wrapper;
using SSO.Common.Constants.Application;
using SSO.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SSO.Application.Features.Tenants.Commands.AddEdit
{
    public record AddEditTenentCommand : IRequest<Result<Guid>>
    {
        public Guid Id { get; set; } = Guid.Empty;
        public string Code { get; set; }        // acme, contoso
        public string Name { get; set; }

        public TenantDatabaseMode DatabaseMode { get; set; }
        public string? DatabaseProvider { get; set; } = string.Empty;
        public string? DatabaseName { get; set; } = string.Empty;
        public string? ConnectionString { get; set; } = string.Empty;

        public string? LogoUrl { get; set; } = string.Empty;
        public string? FaviconUrl { get; set; } = string.Empty;

        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Website { get; set; }
        public string? BillingAddress { get; set; }

        /// <summary>Number of days after invoice due date before SSO access is blocked. Default: 7.</summary>
        public int GracePeriodDays { get; set; } = 7;
        public string Currency { get; set; } = "INR";
        public bool AllowPublicRegistration { get; set; }

        //public Guid SubscriptionId { get; set; }
        public bool IsActive { get; set; }
        public List<Guid>? ClientIds { get; set; } = new List<Guid>();
    }

    public class AddEditTenantValidator : IRequestValidator<AddEditTenentCommand>
    {
        private static readonly Regex TenantCodePattern = new("^[a-z0-9_]+$", RegexOptions.Compiled);
        private static readonly Regex DatabaseNamePattern = new("^[A-Za-z0-9_\\-]+$", RegexOptions.Compiled);
        private static readonly HashSet<string> AllowedCurrencies = new(StringComparer.OrdinalIgnoreCase) { "INR", "USD", "EUR", "GBP" };
        private static readonly HashSet<string> AllowedDatabaseProviders = new(StringComparer.OrdinalIgnoreCase)
        {
            ApplicationConstants.DBProvider.SqlServer,
            ApplicationConstants.DBProvider.Mysql,
            ApplicationConstants.DBProvider.PostgreSql,
            ApplicationConstants.DBProvider.Oracle
        };

        public Task<IEnumerable<ValidationError>> ValidateAsync(AddEditTenentCommand request, CancellationToken cancellationToken)
        {
            var errors = new List<ValidationError>();

            if (string.IsNullOrWhiteSpace(request.Code))
                errors.Add(new ValidationError { PropertyName = nameof(request.Code), ErrorMessage = "Tenant code is required." });
            else if (request.Code.Length > 20)
                errors.Add(new ValidationError { PropertyName = nameof(request.Code), ErrorMessage = "Code cannot exceed 20 characters." });
            else if (!TenantCodePattern.IsMatch(request.Code))
                errors.Add(new ValidationError { PropertyName = nameof(request.Code), ErrorMessage = "Tenant code must contain only lowercase letters, numbers, and underscores." });

            if (string.IsNullOrWhiteSpace(request.Name))
                errors.Add(new ValidationError { PropertyName = nameof(request.Name), ErrorMessage = "Tenant name is required." });
            else if (request.Name.Length > 150)
                errors.Add(new ValidationError { PropertyName = nameof(request.Name), ErrorMessage = "Tenant name cannot exceed 150 characters." });

            if (!string.IsNullOrWhiteSpace(request.Email) && !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(request.Email))
                errors.Add(new ValidationError { PropertyName = nameof(request.Email), ErrorMessage = "Invalid email format." });

            if (!Enum.IsDefined(typeof(TenantDatabaseMode), request.DatabaseMode))
                errors.Add(new ValidationError { PropertyName = nameof(request.DatabaseMode), ErrorMessage = "A valid database mode is required." });

            if (request.DatabaseMode == TenantDatabaseMode.Separate)
            {
                if (string.IsNullOrWhiteSpace(request.DatabaseProvider) || !AllowedDatabaseProviders.Contains(request.DatabaseProvider))
                    errors.Add(new ValidationError { PropertyName = nameof(request.DatabaseProvider), ErrorMessage = "A supported database provider is required for isolated database mode." });

                if (string.IsNullOrWhiteSpace(request.DatabaseName))
                    errors.Add(new ValidationError { PropertyName = nameof(request.DatabaseName), ErrorMessage = "Database name is required for isolated database mode." });
                else if (request.DatabaseName.Length > 128)
                    errors.Add(new ValidationError { PropertyName = nameof(request.DatabaseName), ErrorMessage = "Database name cannot exceed 128 characters." });
                else if (!DatabaseNamePattern.IsMatch(request.DatabaseName))
                    errors.Add(new ValidationError { PropertyName = nameof(request.DatabaseName), ErrorMessage = "Database name can contain only letters, numbers, hyphens, and underscores." });
            }

            if (!string.IsNullOrWhiteSpace(request.Website))
            {
                if (!Uri.TryCreate(request.Website, UriKind.Absolute, out var websiteUri))
                    errors.Add(new ValidationError { PropertyName = nameof(request.Website), ErrorMessage = "Website must be a valid absolute URL." });
                else if (!string.Equals(websiteUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                         !string.Equals(websiteUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                    errors.Add(new ValidationError { PropertyName = nameof(request.Website), ErrorMessage = "Website must use HTTP or HTTPS." });
            }

            if (request.GracePeriodDays < 0)
                errors.Add(new ValidationError { PropertyName = nameof(request.GracePeriodDays), ErrorMessage = "Grace period cannot be negative." });

            if (string.IsNullOrWhiteSpace(request.Currency) || !AllowedCurrencies.Contains(request.Currency))
                errors.Add(new ValidationError { PropertyName = nameof(request.Currency), ErrorMessage = "A supported currency is required." });

            return Task.FromResult(errors.AsEnumerable());
        }
    }
    internal class AddEditTenentCommandHandler : IRequestHandler<AddEditTenentCommand, Result<Guid>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<AddEditTenentCommandHandler> _logger;
        public AddEditTenentCommandHandler(IUnitOfWork<Guid> unitOfWork, ILogger<AddEditTenentCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }


        public async Task<Result<Guid>> Handle(AddEditTenentCommand command, CancellationToken ct = default)
        {
            try
            {
                NormalizeDatabaseConfiguration(command);

                if (command.Id != Guid.Empty)
                {
                    var tenant = await _unitOfWork.Repository<Domain.Entities.Tenants>()
                        .Entities.Include(t => t.TenantClients)
                        .Include(t => t.TenantSubscriptions)
                            .ThenInclude(ts => ts.Subscriptions)
                        .FirstOrDefaultAsync(t => t.Id == command.Id, ct)
                        .ConfigureAwait(false);
                    if (tenant == null)
                        return await Result<Guid>.FailAsync("Tenant not found.");

                    var codeExists = await _unitOfWork.Repository<Domain.Entities.Tenants>().Entities.AnyAsync(x => x.Code == command.Code && x.Id != command.Id, ct);
                    if (codeExists)
                        return await Result<Guid>.FailAsync($"Tenant code '{command.Code}' is already in use.");

                    if (command.DatabaseMode == TenantDatabaseMode.Separate && !TenantAllowsSeparateDatabase(tenant))
                        return await Result<Guid>.FailAsync("This tenant's active subscription does not allow a separate database.");

                    tenant.Code = command.Code;
                    tenant.Name = command.Name;
                    tenant.DatabaseMode = command.DatabaseMode;
                    tenant.DatabaseProvider = command.DatabaseProvider;
                    tenant.DatabaseName = command.DatabaseName;
                    tenant.ConnectionString = null;
                    tenant.IsActive = command.IsActive;
                    tenant.LogoUrl = command.LogoUrl;
                    tenant.FaviconUrl = command.FaviconUrl;
                    tenant.Email = command.Email;
                    tenant.Phone = command.Phone;
                    tenant.Website = command.Website;
                    tenant.BillingAddress = command.BillingAddress;
                    tenant.GracePeriodDays = command.GracePeriodDays;
                    tenant.Currency = command.Currency;
                    tenant.AllowPublicRegistration = command.AllowPublicRegistration;

                    tenant.TenantClients.Clear();
                    if (command.ClientIds != null && command.ClientIds.Any())
                    {
                        foreach (var clientId in command.ClientIds.Distinct())
                        {
                            tenant.TenantClients.Add(new Domain.Entities.TenantClient { TenantId = tenant.Id, ApplicationClientId = clientId });
                        }
                    }

                    await _unitOfWork.Repository<Domain.Entities.Tenants>().UpdateAsync(tenant).ConfigureAwait(false);
                    await _unitOfWork.Commit(ct).ConfigureAwait(false);
                    return await Result<Guid>.SuccessAsync(tenant.Id, "Tenant updated successfully.").ConfigureAwait(false);
                }
                else
                {
                    var codeExists = await _unitOfWork.Repository<Domain.Entities.Tenants>().Entities.AnyAsync(x => x.Code == command.Code, ct);
                    if (codeExists)
                        return await Result<Guid>.FailAsync($"Tenant code '{command.Code}' is already in use.");

                    var newTenant = new Domain.Entities.Tenants
                    {
                        Id = Guid.NewGuid(),
                        Code = command.Code,
                        Name = command.Name,
                        DatabaseMode = command.DatabaseMode,
                        DatabaseProvider = command.DatabaseProvider,
                        DatabaseName = command.DatabaseName,
                        ConnectionString = null,
                        IsActive = command.IsActive,
                        FaviconUrl = command.FaviconUrl!,
                        LogoUrl = command.LogoUrl,
                        Email = command.Email,
                        Phone = command.Phone,
                        Website = command.Website,
                        BillingAddress = command.BillingAddress,
                        GracePeriodDays = command.GracePeriodDays,
                        Currency = command.Currency,
                        AllowPublicRegistration = command.AllowPublicRegistration
                    };

                    if (command.ClientIds != null && command.ClientIds.Any())
                    {
                        foreach (var clientId in command.ClientIds.Distinct())
                        {
                            newTenant.TenantClients.Add(new Domain.Entities.TenantClient { TenantId = newTenant.Id, ApplicationClientId = clientId });
                        }
                    }

                    await _unitOfWork.Repository<Domain.Entities.Tenants>().AddAsync(newTenant).ConfigureAwait(false);
                    await _unitOfWork.Commit(ct).ConfigureAwait(false);
                    return await Result<Guid>.SuccessAsync(newTenant.Id, "Tenant created successfully.").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding/editing tenant with code {Code}", command.Code);
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                if (message.Contains("Duplicate entry") || message.Contains("UNIQUE constraint failed"))
                {
                    return await Result<Guid>.FailAsync($"Tenant code '{command.Code}' is already in use.");
                }
                return await Result<Guid>.FailAsync("An error occurred while processing your request. Please try again later.").ConfigureAwait(false);
            }
        }

        private static void NormalizeDatabaseConfiguration(AddEditTenentCommand command)
        {
            command.DatabaseProvider = command.DatabaseProvider?.Trim();
            command.DatabaseName = command.DatabaseName?.Trim();
            command.ConnectionString = null;

            if (command.DatabaseMode == TenantDatabaseMode.Shared)
            {
                command.DatabaseProvider = null;
                command.DatabaseName = null;
            }
        }

        private static bool TenantAllowsSeparateDatabase(Domain.Entities.Tenants tenant)
        {
            var hasActiveSubscription = tenant.TenantSubscriptions?.Any(ts => ts.IsActive && ts.Subscriptions != null) == true;
            if (!hasActiveSubscription)
            {
                return true;
            }

            return tenant.TenantSubscriptions!.Any(ts => ts.IsActive && ts.Subscriptions?.AllowSeparateDb == true);
        }
    }
}
