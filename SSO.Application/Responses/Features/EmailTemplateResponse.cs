using SSO.Domain.Enums;

namespace SSO.Application.Responses.Features
{
    public class EmailTemplateResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string Subject { get; set; } = default!;
        public EmailTemplateType TemplateType { get; set; }
        public EmailTriggerEvent TriggerEvent { get; set; }
        public string Body { get; set; } = default!;
        public bool IsActive { get; set; }
        public DateTime? CreatedOn { get; set; }
    }
}
