using SSO.Domain.Enums;
using System;

namespace SSO.Application.Responses.Features
{
    public class TemplateResponse
    {
        public Guid Id { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public DocumentType Type { get; set; }
        public string Version { get; set; }
        public string Content { get; set; }
    }
}
