using System.Collections.Generic;

namespace SSO.Application.Responses.Features
{
    public class ImportScopesResponse
    {
        public int ImportedCount { get; set; }
        public List<string> SkippedPermissions { get; set; } = new List<string>();
    }
}
