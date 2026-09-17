using Microsoft.EntityFrameworkCore;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Interfaces.Services;
using SSO.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Infrastructure.Services.OpenXml
{
  
    public sealed class TemplateRepository : ITemplateRepository
    {
        private readonly IUnitOfWork<Guid> _db;

        public TemplateRepository(IUnitOfWork<Guid> db)
        {
            _db = db;
        }

        public Task<Template?> FindByKeyAsync(string key, CancellationToken ct)
        {
            return _db.Repository<Template>().Entities.FirstOrDefaultAsync(t => t.Key == key, ct);
        }
    }
}
