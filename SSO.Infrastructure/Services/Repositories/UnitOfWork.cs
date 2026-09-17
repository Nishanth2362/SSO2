using LazyCache;
using Microsoft.EntityFrameworkCore.Storage;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Interfaces.Services;
using SSO.Domain.Contract;
using SSO.Infrastructure.Contexts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SSO.Infrastructure.Services.Repositories
{
    public class UnitOfWork<TId> : IUnitOfWork<TId>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ApplicationDbContext _dbContext;
        private bool disposed;
        private Hashtable _repositories;
        private readonly IAppCache _cache;
        private IDbContextTransaction? _transaction;
        public UnitOfWork(ApplicationDbContext dbContext, ICurrentUserService currentUserService, IAppCache cache)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _currentUserService = currentUserService;
            _cache = cache;
        }

        public IRepositoryAsync<TEntity, TId> Repository<TEntity>() where TEntity : AuditableEntity<TId>
        {
            _repositories ??= new Hashtable();
            string type = typeof(TEntity).Name;

            if (!_repositories.ContainsKey(type))
            {
                Type repositoryType = typeof(RepositoryAsync<,>);

                object? repositoryInstance = Activator.CreateInstance(repositoryType.MakeGenericType(typeof(TEntity), typeof(TId)), _dbContext);

                _repositories.Add(type, repositoryInstance);
            }

            return (IRepositoryAsync<TEntity, TId>)_repositories[type];
        }

        public async Task<int> Commit(CancellationToken cancellationToken)
        {
            var result= await _dbContext.SaveChangesAsync(cancellationToken);
            if (_transaction != null)
            {
                await _transaction.CommitAsync(cancellationToken);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
            return result;
        }

        public async Task<int> CommitAndRemoveCache(CancellationToken cancellationToken, params string[] cacheKeys)
        {
            int result = await _dbContext.SaveChangesAsync(cancellationToken);
            foreach (string cacheKey in cacheKeys)
            {
                _cache.Remove(cacheKey);
            }
            if (_transaction != null)
            {
                await _transaction.CommitAsync(cancellationToken);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
            return result;
        }

        public async Task Rollback()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync(CancellationToken.None);
                await _transaction.DisposeAsync();
                _transaction = null;
            }

            // Optional: clear EF tracking after rollback
            _dbContext.ChangeTracker.Clear();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    //dispose managed resources
                    _dbContext.Dispose();
                }
            }
            //dispose unmanaged resources
            disposed = true;
        }

        public async Task BeginTransactionAsync(CancellationToken ct)
        {
            if (_transaction != null) return;
            _transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        }
    }
}
