using SSO.Common.Constants.Localization;
using SSO.Common.Settings;

namespace SSO.WebApplication.Settings
{
    public record ServerPreference : IPreference
    {
        public string LanguageCode { get; set; } = LocalizationConstants.SupportedLanguages.FirstOrDefault()?.Code ?? "en-US";

        //TODO - add server preferences
    }
}
