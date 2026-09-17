using SSO.Application.Interfaces.Serialization.Options;
using System.Text.Json;

namespace SSO.Application.Serialization.Options
{
    public class SystemTextJsonOptions : IJsonSerializerOptions
    {
        public JsonSerializerOptions JsonSerializerOptions { get; } = new();
    }
}
