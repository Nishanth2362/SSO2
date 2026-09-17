using SSO.Domain.Contract;
using SSO.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace SSO.Domain.Entities
{
    public class Template : AuditableEntity<Guid>
    {
        public string Key { get; set; } = default!;  // Logical key, e.g., "Invoice"
        public string Name { get; set; } = default!; // Friendly name
        public DocumentType Type { get; set; }
        public string Version { get; set; } = "1.0";
        public string Content { get; set; } = default!;
    }
}
