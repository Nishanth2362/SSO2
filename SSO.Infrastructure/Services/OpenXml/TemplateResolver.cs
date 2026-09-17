using SSO.Application.Interfaces.Services;
using SSO.Domain.Entities;

namespace SSO.Infrastructure.Services.OpenXml
{
    public sealed class TemplateResolver : ITemplateResolver
    {
        private readonly ITemplateRepository _repository;

        public TemplateResolver(ITemplateRepository repository)
        {
            _repository = repository;
        }

        public async Task<Template> ResolveAsync(string key, CancellationToken ct)
        {
            var template = await _repository.FindByKeyAsync(key, ct);
            if (template is null)
                throw new InvalidOperationException(
                    $"Template with key '{key}' not found in database");

            return template;
        }
    }
}
