using Microsoft.EntityFrameworkCore;
using SSO.Application.Interfaces.Services;
using SSO.Application.Requests.DataTable;
using SSO.Application.Responses.DataTable;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace SSO.Infrastructure.Services
{
    public class DataTableService : IDataTableService
    {
        public async Task<DataTableResponse<TDto>> BuildAsync<TEntity, TDto>(
            IQueryable<TEntity> query,
            DataTableRequest request,
            Expression<Func<TEntity, TDto>> selector,
            Expression<Func<TEntity, bool>>? extraFilter = null,
            List<string>? allowedSortColumns = null,
            CancellationToken cancellationToken = default)
            where TEntity : class
        {
            if (extraFilter != null)
                query = query.Where(extraFilter);

            var total = await query.CountAsync(cancellationToken);

            // Search
            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                var search = request.SearchValue.ToLower();

                var parameter = Expression.Parameter(typeof(TEntity), "x");

                var stringProperties = typeof(TEntity)
                    .GetProperties()
                    .Where(p => p.PropertyType == typeof(string));

                Expression? combinedExpression = null;

                foreach (var property in stringProperties)
                {
                    var propertyAccess = Expression.Property(parameter, property);

                    var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
                    var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });

                    var notNull = Expression.NotEqual(propertyAccess, Expression.Constant(null));
                    var toLowerCall = Expression.Call(propertyAccess, toLowerMethod!);
                    var searchConstant = Expression.Constant(search);
                    var containsCall = Expression.Call(toLowerCall, containsMethod!, searchConstant);

                    var propertyExpression = Expression.AndAlso(notNull, containsCall);

                    combinedExpression = combinedExpression == null
                        ? propertyExpression
                        : Expression.OrElse(combinedExpression, propertyExpression);
                }

                if (combinedExpression != null)
                {
                    var lambda = Expression.Lambda<Func<TEntity, bool>>(combinedExpression, parameter);
                    query = query.Where(lambda);
                }
            }

            var filtered = await query.CountAsync(cancellationToken);

            // Sorting
            if (!string.IsNullOrEmpty(request.SortColumn) &&
                (allowedSortColumns == null ||
                 allowedSortColumns.Contains(request.SortColumn)))
            {
                query = request.SortDirection == "asc"
                    ? query.OrderBy(e => EF.Property<object>(e, request.SortColumn))
                    : query.OrderByDescending(e => EF.Property<object>(e, request.SortColumn));
            }

            var data = await query
                .AsNoTracking()
                .Skip(request.Start)
                .Take(request.Length)
                .Select(selector)                
                .ToListAsync(cancellationToken);

            return new DataTableResponse<TDto>
            {
                Draw = request.Draw,
                RecordsTotal = total,
                RecordsFiltered = filtered,
                Data = data
            };
        }
    }
}
