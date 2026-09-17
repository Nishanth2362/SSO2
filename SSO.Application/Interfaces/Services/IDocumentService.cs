using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Interfaces.Services
{
    public interface IDocumentService
    {
        Task<byte[]> ExportAsync<TModel>(
                    string templateKey,
                    TModel model,
                    CancellationToken ct);
    }
}
