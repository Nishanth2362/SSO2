using DocumentFormat.OpenXml.Linq;
using DocumentFormat.OpenXml.Packaging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SSO.Application.Helper
{
    public static class OpenXmlTokenScanner
    {
        private static readonly Regex TokenRegex =
            new(@"\{\{([a-zA-Z0-9\._]+)\}\}", RegexOptions.Compiled);

        public static IReadOnlySet<string> Scan(OpenXmlPackage document)
        {
            var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var part in document.Parts)
            {
                if (part.OpenXmlPart is not OpenXmlPart oxPart) continue;

                var xdoc = oxPart.GetXDocument();
                if (xdoc == null) continue;

                foreach (var text in xdoc.Descendants(X.t))
                {
                    foreach (Match match in TokenRegex.Matches(text.Value))
                    {
                        tokens.Add(match.Groups[1].Value);
                    }
                }
            }

            return tokens;
        }
    }
}
