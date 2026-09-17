using SSO.Application.Requests.DataTable;
using SSO.Application.Responses.DataTable;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace SSO.Application.Interfaces.Services
{
    public interface IDataTableService
    {
        Task<DataTableResponse<TDto>> BuildAsync<TEntity, TDto>(
            IQueryable<TEntity> query,
            DataTableRequest request,
            Expression<Func<TEntity, TDto>> selector,
            Expression<Func<TEntity, bool>>? extraFilter = null,
            List<string>? allowedSortColumns = null,
            CancellationToken cancellationToken = default)
            where TEntity : class;

        /// <summary>
        /// Builds a full (un-paginated) export dataset, applying filters + search,
        /// and optionally restricting to a specific set of IDs (for smart export).
        /// </summary>
        Task<List<TDto>> BuildExportAsync<TEntity, TDto>(
            IQueryable<TEntity> query,
            DataTableRequest request,
            Expression<Func<TEntity, TDto>> selector,
            string idPropertyName,
            Expression<Func<TEntity, bool>>? extraFilter = null,
            CancellationToken cancellationToken = default)
            where TEntity : class;
    }
}
