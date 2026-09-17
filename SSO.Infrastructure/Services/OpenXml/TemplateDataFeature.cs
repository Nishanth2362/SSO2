using SSO.Application.Interfaces.Services;

namespace SSO.Infrastructure.Services.OpenXml
{
    public sealed class TemplateDataFeature : ITemplateDataFeature
    {
        public TemplateDataFeature()
        {
            Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public Dictionary<string, string> Values { get; }
    }

}
