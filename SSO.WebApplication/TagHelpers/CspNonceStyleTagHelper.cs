using Microsoft.AspNetCore.Razor.TagHelpers;

namespace SSO.WebApplication.TagHelpers
{
    [HtmlTargetElement("style")]
    public class CspNonceStyleTagHelper : TagHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CspNonceStyleTagHelper(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (output.Attributes.ContainsName("nonce"))
            {
                return;
            }

            var nonce = _httpContextAccessor.HttpContext?.Items["CSPNonce"]?.ToString();
            if (!string.IsNullOrWhiteSpace(nonce))
            {
                output.Attributes.SetAttribute("nonce", nonce);
            }
        }
    }
}
