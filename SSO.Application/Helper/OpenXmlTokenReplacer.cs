using DocumentFormat.OpenXml.Linq;
using DocumentFormat.OpenXml.Packaging;

namespace SSO.Application.Helper
{
    public static class OpenXmlTokenReplacer
    {
        public static void Apply(OpenXmlPackage document, IDictionary<string, string> values)
        {
            foreach (var part in document.Parts)
            {
                if (part.OpenXmlPart is not OpenXmlPart oxPart) continue;

                var xdoc = oxPart.GetXDocument();
                if (xdoc == null) continue;

                foreach (var text in xdoc.Descendants(X.t))
                {
                    foreach (var kv in values)
                    {
                        text.Value = text.Value.Replace($"{{{{{kv.Key}}}}}", kv.Value);
                    }
                }

                oxPart.SetXDocument(xdoc);
            }
        }
    }
}
