using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Requests.Features
{
    public class ScopeRequest
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public List<Permissions> Permissions { get; set; } = new List<Permissions>();
    }
}
