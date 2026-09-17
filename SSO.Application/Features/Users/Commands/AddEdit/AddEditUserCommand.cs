using System.ComponentModel.DataAnnotations;
using DocumentFormat.OpenXml.Presentation;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using SSO.Application.Features.Roles.Commands.AddEdit;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Interfaces.Services;
using SSO.Application.Interfaces.Services.Features;
using SSO.Application.Requests;
using SSO.Common.Wrapper;
using SSO.Domain.Entities;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using static SSO.Common.Constants.Permission.Permissions;

namespace SSO.Application.Features.Users.Commands.AddEdit
{
    public record AddEditUserCommand : IRequest<Result<Guid>>
    {
        public Guid? Id { get; set; }
        public string Name { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public Guid TenantId { get; set; }
        public bool ActivateUser { get; set; }
        public bool AutoConfirmEmail { get; set; }
        public string? Origin { get; set; }
        public List<Guid> ClientIds { get; set; } = new List<Guid>();
        public List<string> RoleNames { get; set; } = new List<string>();

        /// <summary>Mandatory change description when editing an existing user.</summary>
        public string? Remarks { get; set; }
    }

    public class AddEditUserValidator : IRequestValidator<AddEditUserCommand>
    {
        private static readonly Regex UserNamePattern = new("^[A-Za-z0-9._@-]+$", RegexOptions.Compiled);

        public Task<IEnumerable<ValidationError>> ValidateAsync(AddEditUserCommand request, CancellationToken cancellationToken)
        {
            var errors = new List<ValidationError>();

            if (string.IsNullOrWhiteSpace(request.Name))
                errors.Add(new ValidationError { PropertyName = nameof(request.Name), ErrorMessage = "Full name is required." });
            else if (request.Name.Length > 150)
                errors.Add(new ValidationError { PropertyName = nameof(request.Name), ErrorMessage = "Full name cannot exceed 150 characters." });

            if (string.IsNullOrWhiteSpace(request.UserName))
                errors.Add(new ValidationError { PropertyName = nameof(request.UserName), ErrorMessage = "Username is required." });
            else
            {
                if (request.UserName.Length > 100)
                    errors.Add(new ValidationError { PropertyName = nameof(request.UserName), ErrorMessage = "Username cannot exceed 100 characters." });

                if (!UserNamePattern.IsMatch(request.UserName))
                    errors.Add(new ValidationError { PropertyName = nameof(request.UserName), ErrorMessage = "Username contains unsupported characters." });
            }

            if (string.IsNullOrWhiteSpace(request.Email))
                errors.Add(new ValidationError { PropertyName = nameof(request.Email), ErrorMessage = "Email address is required." });
            else if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(request.Email))
                errors.Add(new ValidationError { PropertyName = nameof(request.Email), ErrorMessage = "Invalid email format." });

            if (request.TenantId == Guid.Empty)
                errors.Add(new ValidationError { PropertyName = nameof(request.TenantId), ErrorMessage = "Tenant selection is required." });

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && request.PhoneNumber.Length > 25)
                errors.Add(new ValidationError { PropertyName = nameof(request.PhoneNumber), ErrorMessage = "Phone number cannot exceed 25 characters." });

            if (request.Id == null || request.Id == Guid.Empty)
            {
                if (string.IsNullOrWhiteSpace(request.Password))
                    errors.Add(new ValidationError { PropertyName = nameof(request.Password), ErrorMessage = "Password is required when creating a user." });
                else if (request.Password.Length < 8)
                    errors.Add(new ValidationError { PropertyName = nameof(request.Password), ErrorMessage = "Password must be at least 8 characters long." });
            }

            if (!request.AutoConfirmEmail && string.IsNullOrWhiteSpace(request.Origin))
                errors.Add(new ValidationError { PropertyName = nameof(request.Origin), ErrorMessage = "A request origin is required when email confirmation is enabled." });

            if (request.Id != null && request.Id != Guid.Empty)
            {
                if (string.IsNullOrWhiteSpace(request.Remarks))
                    errors.Add(new ValidationError { PropertyName = nameof(request.Remarks), ErrorMessage = "A change description (remarks) is required when editing." });
                else if (request.Remarks.Trim().Length < 5)
                    errors.Add(new ValidationError { PropertyName = nameof(request.Remarks), ErrorMessage = "Remarks must be at least 5 characters." });
                else if (request.Remarks.Length > 500)
                    errors.Add(new ValidationError { PropertyName = nameof(request.Remarks), ErrorMessage = "Remarks cannot exceed 500 characters." });
            }

            return Task.FromResult(errors.AsEnumerable());
        }
    }
    internal class AddEditUserCommandHandler : IRequestHandler<AddEditUserCommand, Result<Guid>>
    {
        private readonly ILogger<AddEditRolesCommandHandler> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMailService _mailService;
        private readonly IOpenIddictAuthorizationManager _authorizationManager;        
        private readonly IRoleService _roleService;
        private readonly IEmailTemplateService _emailTemplateService;

        public AddEditUserCommandHandler(
            ILogger<AddEditRolesCommandHandler> logger, 
            UserManager<ApplicationUser> userManager, 
            IMailService mailService,
            IOpenIddictAuthorizationManager authorizationManager, 
            IRoleService roleService,
            IEmailTemplateService emailTemplateService)
        {
            _logger = logger;
            _userManager = userManager;
            _mailService = mailService;
            _roleService = roleService;
            _authorizationManager = authorizationManager;
            _emailTemplateService = emailTemplateService;
        }
        public async Task<Result<Guid>> Handle(AddEditUserCommand request, CancellationToken ct)
        {
            try
            {
                if (request.Id == null || request.Id == Guid.Empty)
                {
                    // Create logic
                    var userWithSameUserName = await _userManager!.FindByNameAsync(request.UserName);
                    if (userWithSameUserName != null)
                        return await Result<Guid>.FailAsync($"Username {request.UserName} is already taken.");

                    var userWithSameEmail = await _userManager.FindByEmailAsync(request.Email);
                    if (userWithSameEmail != null)
                        return await Result<Guid>.FailAsync($"Email {request.Email} is already registered.");

                    ApplicationUser user = new()
                    {
                        Email = request.Email,
                        Name = request.Name,
                        UserName = request.UserName,
                        PhoneNumber = request.PhoneNumber,
                        IsActive = request.ActivateUser,
                        EmailConfirmed = request.AutoConfirmEmail,
                        TenantId = request.TenantId
                    };

                    IdentityResult result = await _userManager.CreateAsync(user, request.Password);
                    if (result.Succeeded)
                    {
                        // Map Roles
                        if (request.RoleNames != null && request.RoleNames.Any())
                        {
                            await _roleService.AddUserRoles(user, request.RoleNames);
                        }

                        // Map Client Consents
                        if (request.ClientIds != null && request.ClientIds.Any())
                        {
                            foreach (var clientId in request.ClientIds)
                            {
                                await _authorizationManager.CreateAsync(new OpenIddictAuthorizationDescriptor
                                {
                                    Subject = user.Id.ToString(),
                                    ApplicationId = clientId.ToString(),
                                    Status = OpenIddictConstants.Statuses.Valid,
                                    Type = OpenIddictConstants.AuthorizationTypes.Permanent
                                }, ct);
                            }
                        }

                        if (!request.AutoConfirmEmail)
                        {
                            string verificationUri = await SendVerificationEmail(user, request.Origin!);

                            var tokens = new Dictionary<string, string>
                            {
                                ["UserName"]   = user.Name ?? user.UserName ?? "User",
                                ["Email"]      = user.Email ?? string.Empty,
                                ["AppName"]    = "Schoola",
                                ["TenantName"] = "Schoola",
                                ["CallbackUrl"] = verificationUri,
                                ["LogoUrl"]    = string.Empty
                            };

                            var emailTemplate = await _emailTemplateService.RenderAsync(Domain.Enums.EmailTriggerEvent.EmailConfirmation, tokens);

                            MailRequest mailRequest = new()
                            {
                                From = "support@auxinz.io",
                                To = new List<string>() { user.Email },
                                Body = !string.IsNullOrWhiteSpace(emailTemplate?.Body) ? emailTemplate.Value.Body : string.Format("Please confirm your account by <a href='{0}'>clicking here</a>.", verificationUri),
                                Subject = !string.IsNullOrWhiteSpace(emailTemplate?.Subject) ? emailTemplate.Value.Subject : "Confirm Registration"
                            };
                            _ = BackgroundJob.Enqueue(() => _mailService.SendAsync(mailRequest));
                        }
                        else
                        {
                            // Send Invitation Email
                            var tokens = new Dictionary<string, string>
                            {
                                ["UserName"]   = user.Name ?? user.UserName ?? "User",
                                ["Email"]      = user.Email ?? string.Empty,
                                ["AppName"]    = "Schoola",
                                ["TenantName"] = "Schoola",
                                ["LogoUrl"]    = string.Empty
                            };

                            var emailTemplate = await _emailTemplateService.RenderAsync(Domain.Enums.EmailTriggerEvent.Invitation, tokens);

                            MailRequest mailRequest = new()
                            {
                                From = null, // Automatically resolved from MailConfiguration:From in AppSettings,
                                To = new List<string>() { user.Email },
                                Body = !string.IsNullOrWhiteSpace(emailTemplate?.Body) ? emailTemplate.Value.Body : "Welcome! You have been invited to join the platform.",
                                Subject = !string.IsNullOrWhiteSpace(emailTemplate?.Subject) ? emailTemplate.Value.Subject : "Invitation to join"
                            };
                            _ = BackgroundJob.Enqueue(() => _mailService.SendAsync(mailRequest));
                        }

                        return await Result<Guid>.SuccessAsync(user.Id, $"User {user.UserName} created successfully.");
                    }
                    else
                    {
                        return await Result<Guid>.FailAsync(result.Errors.Select(a => a.Description).ToList());
                    }
                }
                else
                {
                    // Update logic
                    var user = await _userManager.FindByIdAsync(request.Id.ToString()!);
                    if (user == null) return await Result<Guid>.FailAsync("User not found.");

                    // Check if new username is taken by another user
                    if (!string.Equals(user.UserName, request.UserName, StringComparison.OrdinalIgnoreCase))
                    {
                        var setUserNameResult = await _userManager.SetUserNameAsync(user, request.UserName);
                        if (!setUserNameResult.Succeeded)
                        {
                            return await Result<Guid>.FailAsync(setUserNameResult.Errors.Select(e => e.Description).ToList());
                        }
                    }

                    // Check if new email is taken by another user
                    if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
                    {
                        var setEmailResult = await _userManager.SetEmailAsync(user, request.Email);
                        if (!setEmailResult.Succeeded)
                        {
                            return await Result<Guid>.FailAsync(setEmailResult.Errors.Select(e => e.Description).ToList());
                        }
                    }

                    user.Name = request.Name;
                    user.PhoneNumber = request.PhoneNumber;
                    user.IsActive = request.ActivateUser;
                    user.EmailConfirmed = request.AutoConfirmEmail;
                    user.TenantId = request.TenantId;

                    var result = await _userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        // Update Roles
                        
                        await _roleService.RemoveAllUserRoles(user);
                        if (request.RoleNames != null && request.RoleNames.Any())
                        {
                            await _roleService.AddUserRoles(user, request.RoleNames);
                        }

                        // Update Client Consents (Authorization logic might need specialized sync depending on business rules, here we just append new ones if not present or you might want to wipe and re-create)
                        // For simplicity in this Task, we append consents.
                        if (request.ClientIds != null && request.ClientIds.Any())
                        {
                            foreach (var clientId in request.ClientIds)
                            {
                                var authorizations = _authorizationManager.FindAsync(
                                    subject: user.Id.ToString(),
                                    client: null, // Check by App ID later in loop
                                    status: OpenIddictConstants.Statuses.Valid,
                                    type: OpenIddictConstants.AuthorizationTypes.Permanent,
                                    scopes: null);
                                
                                bool exists = false;
                                await foreach (var auth in authorizations)
                                {
                                    if (await _authorizationManager.GetApplicationIdAsync(auth) == clientId.ToString())
                                    {
                                        exists = true;
                                        break;
                                    }
                                }

                                if (!exists)
                                {
                                    await _authorizationManager.CreateAsync(new OpenIddictAuthorizationDescriptor
                                    {
                                        Subject = user.Id.ToString(),
                                        ApplicationId = clientId.ToString(),                                        
                                        Status = OpenIddictConstants.Statuses.Valid,
                                        Type = OpenIddictConstants.AuthorizationTypes.Permanent
                                    }, ct);
                                }
                            }
                        }

                        return await Result<Guid>.SuccessAsync(user.Id, "User updated successfully.");
                    }
                    else
                    {
                        return await Result<Guid>.FailAsync(result.Errors.Select(a => a.Description).ToList());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while handling AddEditUserCommand.");
                return await Result<Guid>.FailAsync("An error occurred while processing your request.");
            }
        }

        private async Task<string> SendVerificationEmail(ApplicationUser user, string origin)
        {
            string code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            string route = "api/identity/user/confirm-email/";
            Uri endpointUri = new(string.Concat($"{origin}/", route));
            string verificationUri = QueryHelpers.AddQueryString(endpointUri.ToString(), "userId", user.Id.ToString());
            verificationUri = QueryHelpers.AddQueryString(verificationUri, "code", code);
            return verificationUri;
        }
    }
}
