using SSO.Domain.Enums;

namespace SSO.Application.Interfaces.Services;

/// <summary>
/// Resolves an active email template by trigger event and replaces
/// handlebars tokens with the supplied values.
/// Returns null if no active template exists for the requested event
/// (callers should fall back to a hardcoded default).
/// </summary>
public interface IEmailTemplateService
{
    /// <summary>
    /// Fetch and render an email template for the given trigger event.
    /// </summary>
    /// <param name="triggerEvent">The event that fires this email.</param>
    /// <param name="tokens">Key/value pairs for {{Token}} replacement.</param>
    /// <returns>Rendered (Subject, Body) or null when no template is active.</returns>
    Task<(string Subject, string Body)?> RenderAsync(
        EmailTriggerEvent triggerEvent,
        Dictionary<string, string> tokens);
}
