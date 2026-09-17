using SSO.Domain.Contract;
using SSO.Domain.Enums;

namespace SSO.Domain.Entities
{
    public class EmailTemplate : AuditableEntity<Guid>
    {
        public string Name { get; set; } = default!;

        public string Subject { get; set; } = default!;

        public EmailTemplateType TemplateType { get; set; } = EmailTemplateType.HTML;

        public EmailTriggerEvent TriggerEvent { get; set; } = EmailTriggerEvent.Registration;

        public string Body { get; set; } = default!;

        public bool IsActive { get; set; } = true;
    }
}
