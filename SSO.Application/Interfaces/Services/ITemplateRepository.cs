using SSO.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Interfaces.Services
{
    public interface ITemplateRepository
    {
        Task<Template?> FindByKeyAsync(string key, CancellationToken ct);
    }

}
