using Newtonsoft.Json;
using SSO.Application.Interfaces.Serialization.Settings;

namespace SSO.Application.Serialization.Settings
{
    public class NewtonsoftJsonSettings : IJsonSerializerSettings
    {
        public JsonSerializerSettings JsonSerializerSettings { get; } = new();
    }
}
