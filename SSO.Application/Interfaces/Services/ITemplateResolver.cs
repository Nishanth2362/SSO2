using SSO.Domain.Entities;

namespace SSO.Application.Interfaces.Services
{
    public interface ITemplateResolver
    {
        Task<Template> ResolveAsync(string key, CancellationToken ct);
    }
}
