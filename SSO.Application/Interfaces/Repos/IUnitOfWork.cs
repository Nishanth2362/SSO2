using SSO.Domain.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSO.Application.Interfaces.Repos
{
    public interface IUnitOfWork<TId> : IDisposable
    {
        Task BeginTransactionAsync(CancellationToken cancellationToken);
        IRepositoryAsync<T, TId> Repository<T>() where T : AuditableEntity<TId>;
        Task<int> Commit(CancellationToken cancellationToken);

        Task<int> CommitAndRemoveCache(CancellationToken cancellationToken, params string[] cacheKeys);

        Task Rollback();
    }
}
