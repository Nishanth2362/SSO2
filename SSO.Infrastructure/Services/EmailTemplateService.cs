using Microsoft.EntityFrameworkCore;
using SSO.Application.Interfaces.Services;
using SSO.Domain.Enums;
using SSO.Infrastructure.Contexts;

namespace SSO.Infrastructure.Services;

/// <summary>
/// Fetches the first active <see cref="Domain.Entities.EmailTemplate"/> for a trigger event,
/// replaces {{Token}} placeholders, and returns the rendered subject + body.
/// </summary>
public class EmailTemplateService : IEmailTemplateService
{
    private readonly ApplicationDbContext _db;

    public EmailTemplateService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(string Subject, string Body)?> RenderAsync(
        EmailTriggerEvent triggerEvent,
        Dictionary<string, string> tokens)
    {
        var template = await _db.EmailTemplates
            .AsNoTracking()
            .Where(t => t.TriggerEvent == triggerEvent && t.IsActive)
            .OrderByDescending(t => t.CreatedOn)   
            .FirstOrDefaultAsync();

        if (template is null)
            return null;

        var subject = ReplaceTokens(template.Subject, tokens);
        var body    = ReplaceTokens(template.Body, tokens);

        return (subject, body);
    }

    private static string ReplaceTokens(string text, Dictionary<string, string> tokens)
    {
        foreach (var (key, value) in tokens)
        {
            text = text.Replace(
                "{{" + key + "}}",
                value ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);
        }
        return text;
    }
}
