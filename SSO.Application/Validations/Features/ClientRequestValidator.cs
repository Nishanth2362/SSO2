using SSO.Application.Helper;
using SSO.Application.Requests.Features;
using SSO.Domain.Enums;
using SSO.Shared.Wrapper.Mediator;
using System.Text.RegularExpressions;

namespace SSO.Application.Validations.Features
{
    public class ClientRequestValidator : IRequestValidator<ClientRequest>
    {
        private static readonly Regex ClientIdPattern = new("^[A-Za-z0-9._:-]+$", RegexOptions.Compiled);

        public Task<IEnumerable<ValidationError>> ValidateAsync(ClientRequest request, CancellationToken cancellationToken)
        {
            var errors = new List<ValidationError>();

            if (request == null)
            {
                errors.Add(new ValidationError
                {
                    PropertyName = nameof(ClientRequest),
                    ErrorMessage = "Client request payload is required."
                });

                return Task.FromResult(errors.AsEnumerable());
            }

            if (string.IsNullOrWhiteSpace(request.ClientId))
            {
                errors.Add(new ValidationError { PropertyName = nameof(request.ClientId), ErrorMessage = "Client ID is required." });
            }
            else
            {
                if (request.ClientId.Length > 100)
                    errors.Add(new ValidationError { PropertyName = nameof(request.ClientId), ErrorMessage = "Client ID cannot exceed 100 characters." });

                if (!ClientIdPattern.IsMatch(request.ClientId))
                    errors.Add(new ValidationError { PropertyName = nameof(request.ClientId), ErrorMessage = "Client ID may only contain letters, numbers, dot, underscore, colon, and hyphen." });
            }

            if (string.IsNullOrWhiteSpace(request.ClientName))
            {
                errors.Add(new ValidationError { PropertyName = nameof(request.ClientName), ErrorMessage = "Client name is required." });
            }
            else if (request.ClientName.Length > 200)
            {
                errors.Add(new ValidationError { PropertyName = nameof(request.ClientName), ErrorMessage = "Client name cannot exceed 200 characters." });
            }

            if (!Enum.IsDefined(typeof(ClientType), request.ClientType))
                errors.Add(new ValidationError { PropertyName = nameof(request.ClientType), ErrorMessage = "A valid client type is required." });

            if (!string.IsNullOrWhiteSpace(request.Audience) && request.Audience.Length > 200)
                errors.Add(new ValidationError { PropertyName = nameof(request.Audience), ErrorMessage = "Audience cannot exceed 200 characters." });

            request.RedirectUris ??= new List<string>();
            request.PostLogoutRedirectUris ??= new List<string>();
            request.Scopes ??= new List<ScopeRequest>();

            if (request.ClientType != ClientType.Machine && !request.RedirectUris.Any(uri => !string.IsNullOrWhiteSpace(uri)))
            {
                errors.Add(new ValidationError
                {
                    PropertyName = nameof(request.RedirectUris),
                    ErrorMessage = "At least one redirect URI is required for interactive applications."
                });
            }

            ValidateUris(request.RedirectUris, nameof(request.RedirectUris), "redirect URI", errors);
            ValidateUris(request.PostLogoutRedirectUris, nameof(request.PostLogoutRedirectUris), "post-logout redirect URI", errors);
            ValidateScopes(request.Scopes, errors);

            if (request.ClientType.RequiresClientSecret() &&
                !string.IsNullOrWhiteSpace(request.ClientSecret) &&
                request.ClientSecret.Length < 16)
            {
                errors.Add(new ValidationError
                {
                    PropertyName = nameof(request.ClientSecret),
                    ErrorMessage = "Client secret must be at least 16 characters when provided."
                });
            }

            return Task.FromResult(errors.AsEnumerable());
        }

        private static void ValidateUris(IEnumerable<string> uris, string propertyName, string label, ICollection<ValidationError> errors)
        {
            var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var uri in uris.Where(u => !string.IsNullOrWhiteSpace(u)))
            {
                var trimmed = uri.Trim();
                if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var parsed) ||
                    (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
                {
                    errors.Add(new ValidationError
                    {
                        PropertyName = propertyName,
                        ErrorMessage = $"The value '{trimmed}' is not a valid absolute {label}."
                    });
                    continue;
                }

                if (!normalized.Add(trimmed))
                {
                    errors.Add(new ValidationError
                    {
                        PropertyName = propertyName,
                        ErrorMessage = $"Duplicate {label}s are not allowed."
                    });
                    break;
                }
            }
        }

        private static void ValidateScopes(IEnumerable<ScopeRequest> scopes, ICollection<ValidationError> errors)
        {
            var scopeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;

            foreach (var scope in scopes)
            {
                var scopePath = $"{nameof(ClientRequest.Scopes)}[{index}]";
                if (scope == null)
                {
                    errors.Add(new ValidationError { PropertyName = scopePath, ErrorMessage = "Scope entries cannot be empty." });
                    index++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(scope.Name))
                {
                    errors.Add(new ValidationError { PropertyName = $"{scopePath}.{nameof(scope.Name)}", ErrorMessage = "Scope name is required." });
                }
                else if (!scopeNames.Add(scope.Name.Trim()))
                {
                    errors.Add(new ValidationError { PropertyName = $"{scopePath}.{nameof(scope.Name)}", ErrorMessage = $"Duplicate scope '{scope.Name}' is not allowed." });
                }

                var permissionCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (var permissionIndex = 0; permissionIndex < scope.Permissions.Count; permissionIndex++)
                {
                    var permission = scope.Permissions[permissionIndex];
                    var permissionPath = $"{scopePath}.{nameof(scope.Permissions)}[{permissionIndex}]";

                    if (permission == null)
                    {
                        errors.Add(new ValidationError { PropertyName = permissionPath, ErrorMessage = "Permission entries cannot be empty." });
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(permission.Code))
                    {
                        errors.Add(new ValidationError { PropertyName = $"{permissionPath}.{nameof(permission.Code)}", ErrorMessage = "Permission code is required." });
                    }
                    else if (!permissionCodes.Add(permission.Code.Trim()))
                    {
                        errors.Add(new ValidationError { PropertyName = $"{permissionPath}.{nameof(permission.Code)}", ErrorMessage = $"Duplicate permission code '{permission.Code}' is not allowed within the same scope." });
                    }

                    if (string.IsNullOrWhiteSpace(permission.Description))
                    {
                        errors.Add(new ValidationError { PropertyName = $"{permissionPath}.{nameof(permission.Description)}", ErrorMessage = "Permission description is required." });
                    }
                }

                index++;
            }
        }
    }
}
