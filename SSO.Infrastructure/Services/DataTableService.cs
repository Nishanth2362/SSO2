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

            // Dynamic Date Range Filter
            if (request.StartDate.HasValue || request.EndDate.HasValue)
            {
                var properties = typeof(TEntity).GetProperties();
                var namePriorities = new[] { "PaymentDate", "InvoiceDate", "DateTime", "CreationDate", "StartDateUtc", "CreatedOn" };
                System.Reflection.PropertyInfo? dateProp = null;
                
                foreach (var priority in namePriorities)
                {
                    var prop = properties.FirstOrDefault(p => p.Name.Equals(priority, StringComparison.OrdinalIgnoreCase) && 
                        (p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?)));
                    if (prop != null)
                    {
                        dateProp = prop;
                        break;
                    }
                }
                
                if (dateProp == null)
                {
                    dateProp = properties.FirstOrDefault(p => (p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?)) &&
                        (p.Name.Contains("Date", StringComparison.OrdinalIgnoreCase) || 
                         p.Name.Contains("Time", StringComparison.OrdinalIgnoreCase) || 
                         p.Name.Contains("Created", StringComparison.OrdinalIgnoreCase)));
                }

                if (dateProp != null)
                {
                    var parameter = Expression.Parameter(typeof(TEntity), "x");
                    var propertyAccess = Expression.Property(parameter, dateProp);
                    Expression propExpr = propertyAccess;
                    if (dateProp.PropertyType == typeof(DateTime?))
                    {
                        propExpr = Expression.Property(propertyAccess, "Value");
                    }

                    if (request.StartDate.HasValue)
                    {
                        Expression startFilter;
                        if (dateProp.PropertyType == typeof(DateTime?))
                        {
                            startFilter = Expression.AndAlso(
                                Expression.NotEqual(propertyAccess, Expression.Constant(null)),
                                Expression.GreaterThanOrEqual(propExpr, Expression.Constant(request.StartDate.Value))
                            );
                        }
                        else
                        {
                            startFilter = Expression.GreaterThanOrEqual(propExpr, Expression.Constant(request.StartDate.Value));
                        }
                        var lambda = Expression.Lambda<Func<TEntity, bool>>(startFilter, parameter);
                        query = query.Where(lambda);
                    }

                    if (request.EndDate.HasValue)
                    {
                        var endOfDay = request.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                        Expression endFilter;
                        if (dateProp.PropertyType == typeof(DateTime?))
                        {
                            endFilter = Expression.AndAlso(
                                Expression.NotEqual(propertyAccess, Expression.Constant(null)),
                                Expression.LessThanOrEqual(propExpr, Expression.Constant(endOfDay))
                            );
                        }
                        else
                        {
                            endFilter = Expression.LessThanOrEqual(propExpr, Expression.Constant(endOfDay));
                        }
                        var lambda = Expression.Lambda<Func<TEntity, bool>>(endFilter, parameter);
                        query = query.Where(lambda);
                    }
                }
            }

            var total = await query.CountAsync(cancellationToken);

            // Dynamic Filters
            if (request.Filters != null && request.Filters.Count > 0)
            {
                var properties = typeof(TEntity).GetProperties();
                foreach (var filter in request.Filters)
                {
                    var propName = filter.Key;
                    var propVal = filter.Value;
                    var prop = properties.FirstOrDefault(p => p.Name.Equals(propName, StringComparison.OrdinalIgnoreCase));
                    if (prop != null)
                    {
                        var parameter = Expression.Parameter(typeof(TEntity), "x");
                        var propertyAccess = Expression.Property(parameter, prop);
                        Expression? combinedPropExpr = null;

                        var splitValues = propVal.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if (splitValues.Length == 0) continue;

                        foreach (var val in splitValues)
                        {
                            var trimmedVal = val.Trim();
                            Expression? singleExpr = null;

                            if (prop.PropertyType == typeof(string))
                            {
                                var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
                                var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                                var notNull = Expression.NotEqual(propertyAccess, Expression.Constant(null));
                                var toLowerCall = Expression.Call(propertyAccess, toLowerMethod!);
                                var containsCall = Expression.Call(toLowerCall, containsMethod!, Expression.Constant(trimmedVal.ToLower()));
                                singleExpr = Expression.AndAlso(notNull, containsCall);
                            }
                            else if (prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?))
                            {
                                if (bool.TryParse(trimmedVal, out var bVal))
                                {
                                    Expression valExpr = Expression.Constant(bVal);
                                    if (prop.PropertyType == typeof(bool?))
                                    {
                                        singleExpr = Expression.AndAlso(
                                            Expression.NotEqual(propertyAccess, Expression.Constant(null)),
                                            Expression.Equal(Expression.Property(propertyAccess, "Value"), valExpr)
                                        );
                                    }
                                    else
                                    {
                                        singleExpr = Expression.Equal(propertyAccess, valExpr);
                                    }
                                }
                            }
                            else if (prop.PropertyType == typeof(Guid) || prop.PropertyType == typeof(Guid?))
                            {
                                if (Guid.TryParse(trimmedVal, out var gVal))
                                {
                                    Expression valExpr = Expression.Constant(gVal);
                                    if (prop.PropertyType == typeof(Guid?))
                                    {
                                        singleExpr = Expression.AndAlso(
                                            Expression.NotEqual(propertyAccess, Expression.Constant(null)),
                                            Expression.Equal(Expression.Property(propertyAccess, "Value"), valExpr)
                                        );
                                    }
                                    else
                                    {
                                        singleExpr = Expression.Equal(propertyAccess, valExpr);
                                    }
                                }
                            }
                            else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
                            {
                                if (int.TryParse(trimmedVal, out var iVal))
                                {
                                    Expression valExpr = Expression.Constant(iVal);
                                    if (prop.PropertyType == typeof(int?))
                                    {
                                        singleExpr = Expression.AndAlso(
                                            Expression.NotEqual(propertyAccess, Expression.Constant(null)),
                                            Expression.Equal(Expression.Property(propertyAccess, "Value"), valExpr)
                                        );
                                    }
                                    else
                                    {
                                        singleExpr = Expression.Equal(propertyAccess, valExpr);
                                    }
                                }
                            }
                            else if (prop.PropertyType.IsEnum || (Nullable.GetUnderlyingType(prop.PropertyType)?.IsEnum == true))
                            {
                                var enumType = prop.PropertyType.IsEnum ? prop.PropertyType : Nullable.GetUnderlyingType(prop.PropertyType)!;
                                if (Enum.TryParse(enumType, trimmedVal, true, out var enumVal))
                                {
                                    Expression valExpr = Expression.Constant(enumVal, prop.PropertyType);
                                    singleExpr = Expression.Equal(propertyAccess, valExpr);
                                }
                                else if (int.TryParse(trimmedVal, out var intVal) && Enum.IsDefined(enumType, intVal))
                                {
                                    var enumValFromInt = Enum.ToObject(enumType, intVal);
                                    Expression valExpr = Expression.Constant(enumValFromInt, prop.PropertyType);
                                    singleExpr = Expression.Equal(propertyAccess, valExpr);
                                }
                            }

                            if (singleExpr != null)
                            {
                                combinedPropExpr = combinedPropExpr == null
                                    ? singleExpr
                                    : Expression.OrElse(combinedPropExpr, singleExpr);
                            }
                        }

                        if (combinedPropExpr != null)
                        {
                            var lambda = Expression.Lambda<Func<TEntity, bool>>(combinedPropExpr, parameter);
                            query = query.Where(lambda);
                        }
                    }
                }
            }

            // Search
            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                var search = request.SearchValue.ToLower();

                var parameter = Expression.Parameter(typeof(TEntity), "x");

                var stringProperties = typeof(TEntity)
                    .GetProperties()
                    .Where(p => p.PropertyType == typeof(string));

                if (!string.IsNullOrEmpty(request.SearchColumn) && !request.SearchColumn.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    stringProperties = stringProperties.Where(p => p.Name.Equals(request.SearchColumn, StringComparison.OrdinalIgnoreCase));
                }

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

        public async Task<List<TDto>> BuildExportAsync<TEntity, TDto>(
            IQueryable<TEntity> query,
            DataTableRequest request,
            Expression<Func<TEntity, TDto>> selector,
            string idPropertyName,
            Expression<Func<TEntity, bool>>? extraFilter = null,
            CancellationToken cancellationToken = default)
            where TEntity : class
        {
            if (extraFilter != null)
                query = query.Where(extraFilter);

            // SelectedIds filter (overrides all other filters and pagination, restricts to specific records)
            if (request.SelectedIds != null && request.SelectedIds.Count > 0)
            {
                var idProp = typeof(TEntity).GetProperty(idPropertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase)
                    ?? typeof(TEntity).GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);

                if (idProp != null)
                {
                    var parameter = Expression.Parameter(typeof(TEntity), "x");
                    var propertyAccess = Expression.Property(parameter, idProp);

                    Expression? combinedIdExpr = null;
                    foreach (var idStr in request.SelectedIds)
                    {
                        Expression? singleExpr = null;
                        if (idProp.PropertyType == typeof(Guid) || idProp.PropertyType == typeof(Guid?))
                        {
                            if (Guid.TryParse(idStr, out var gVal))
                            {
                                var valExpr = idProp.PropertyType == typeof(Guid?)
                                    ? Expression.Constant((Guid?)gVal, typeof(Guid?))
                                    : (Expression)Expression.Constant(gVal, typeof(Guid));
                                singleExpr = Expression.Equal(propertyAccess, valExpr);
                            }
                        }
                        else if (idProp.PropertyType == typeof(int) || idProp.PropertyType == typeof(int?))
                        {
                            if (int.TryParse(idStr, out var iVal))
                            {
                                singleExpr = Expression.Equal(propertyAccess, Expression.Constant(iVal));
                            }
                        }
                        else if (idProp.PropertyType == typeof(string))
                        {
                            singleExpr = Expression.Equal(propertyAccess, Expression.Constant(idStr));
                        }

                        if (singleExpr != null)
                            combinedIdExpr = combinedIdExpr == null ? singleExpr : Expression.OrElse(combinedIdExpr, singleExpr);
                    }

                    if (combinedIdExpr != null)
                    {
                        query = query.Where(Expression.Lambda<Func<TEntity, bool>>(combinedIdExpr, parameter));
                    }
                    else
                    {
                        query = query.Where(x => false);
                    }
                }
                else
                {
                    query = query.Where(x => false);
                }

                return await query
                    .AsNoTracking()
                    .Select(selector)
                    .ToListAsync(cancellationToken);
            }

            // Dynamic Date Range Filter
            if (request.StartDate.HasValue || request.EndDate.HasValue)
            {
                var properties = typeof(TEntity).GetProperties();
                var namePriorities = new[] { "PaymentDate", "InvoiceDate", "DateTime", "CreationDate", "StartDateUtc", "CreatedOn" };
                System.Reflection.PropertyInfo? dateProp = null;

                foreach (var priority in namePriorities)
                {
                    var prop = properties.FirstOrDefault(p => p.Name.Equals(priority, StringComparison.OrdinalIgnoreCase) &&
                        (p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?)));
                    if (prop != null) { dateProp = prop; break; }
                }

                if (dateProp == null)
                {
                    dateProp = properties.FirstOrDefault(p => (p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?)) &&
                        (p.Name.Contains("Date", StringComparison.OrdinalIgnoreCase) ||
                         p.Name.Contains("Time", StringComparison.OrdinalIgnoreCase) ||
                         p.Name.Contains("Created", StringComparison.OrdinalIgnoreCase)));
                }

                if (dateProp != null)
                {
                    var parameter = Expression.Parameter(typeof(TEntity), "x");
                    var propertyAccess = Expression.Property(parameter, dateProp);
                    Expression propExpr = propertyAccess;
                    if (dateProp.PropertyType == typeof(DateTime?))
                        propExpr = Expression.Property(propertyAccess, "Value");

                    if (request.StartDate.HasValue)
                    {
                        Expression startFilter = dateProp.PropertyType == typeof(DateTime?)
                            ? Expression.AndAlso(Expression.NotEqual(propertyAccess, Expression.Constant(null)), Expression.GreaterThanOrEqual(propExpr, Expression.Constant(request.StartDate.Value)))
                            : Expression.GreaterThanOrEqual(propExpr, Expression.Constant(request.StartDate.Value));
                        query = query.Where(Expression.Lambda<Func<TEntity, bool>>(startFilter, parameter));
                    }

                    if (request.EndDate.HasValue)
                    {
                        var endOfDay = request.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                        Expression endFilter = dateProp.PropertyType == typeof(DateTime?)
                            ? Expression.AndAlso(Expression.NotEqual(propertyAccess, Expression.Constant(null)), Expression.LessThanOrEqual(propExpr, Expression.Constant(endOfDay)))
                            : Expression.LessThanOrEqual(propExpr, Expression.Constant(endOfDay));
                        query = query.Where(Expression.Lambda<Func<TEntity, bool>>(endFilter, parameter));
                    }
                }
            }

            // Dynamic Filters
            if (request.Filters != null && request.Filters.Count > 0)
            {
                var properties = typeof(TEntity).GetProperties();
                foreach (var filter in request.Filters)
                {
                    var propName = filter.Key;
                    var propVal = filter.Value;
                    var prop = properties.FirstOrDefault(p => p.Name.Equals(propName, StringComparison.OrdinalIgnoreCase));
                    if (prop != null)
                    {
                        var parameter = Expression.Parameter(typeof(TEntity), "x");
                        var propertyAccess = Expression.Property(parameter, prop);
                        Expression? combinedPropExpr = null;

                        var splitValues = propVal.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if (splitValues.Length == 0) continue;

                        foreach (var val in splitValues)
                        {
                            var trimmedVal = val.Trim();
                            Expression? singleExpr = null;

                            if (prop.PropertyType == typeof(string))
                            {
                                var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
                                var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                                var notNull = Expression.NotEqual(propertyAccess, Expression.Constant(null));
                                var toLowerCall = Expression.Call(propertyAccess, toLowerMethod!);
                                var containsCall = Expression.Call(toLowerCall, containsMethod!, Expression.Constant(trimmedVal.ToLower()));
                                singleExpr = Expression.AndAlso(notNull, containsCall);
                            }
                            else if (prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?))
                            {
                                if (bool.TryParse(trimmedVal, out var bVal))
                                {
                                    Expression valExpr = Expression.Constant(bVal);
                                    singleExpr = prop.PropertyType == typeof(bool?)
                                        ? Expression.AndAlso(Expression.NotEqual(propertyAccess, Expression.Constant(null)), Expression.Equal(Expression.Property(propertyAccess, "Value"), valExpr))
                                        : Expression.Equal(propertyAccess, valExpr);
                                }
                            }
                            else if (prop.PropertyType == typeof(Guid) || prop.PropertyType == typeof(Guid?))
                            {
                                if (Guid.TryParse(trimmedVal, out var gVal))
                                {
                                    Expression valExpr = Expression.Constant(gVal);
                                    singleExpr = prop.PropertyType == typeof(Guid?)
                                        ? Expression.AndAlso(Expression.NotEqual(propertyAccess, Expression.Constant(null)), Expression.Equal(Expression.Property(propertyAccess, "Value"), valExpr))
                                        : Expression.Equal(propertyAccess, valExpr);
                                }
                            }
                            else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
                            {
                                if (int.TryParse(trimmedVal, out var iVal))
                                {
                                    Expression valExpr = Expression.Constant(iVal);
                                    singleExpr = prop.PropertyType == typeof(int?)
                                        ? Expression.AndAlso(Expression.NotEqual(propertyAccess, Expression.Constant(null)), Expression.Equal(Expression.Property(propertyAccess, "Value"), valExpr))
                                        : Expression.Equal(propertyAccess, valExpr);
                                }
                            }
                            else if (prop.PropertyType.IsEnum || (Nullable.GetUnderlyingType(prop.PropertyType)?.IsEnum == true))
                            {
                                var enumType = prop.PropertyType.IsEnum ? prop.PropertyType : Nullable.GetUnderlyingType(prop.PropertyType)!;
                                if (Enum.TryParse(enumType, trimmedVal, true, out var enumVal))
                                {
                                    singleExpr = Expression.Equal(propertyAccess, Expression.Constant(enumVal, prop.PropertyType));
                                }
                                else if (int.TryParse(trimmedVal, out var intVal) && Enum.IsDefined(enumType, intVal))
                                {
                                    singleExpr = Expression.Equal(propertyAccess, Expression.Constant(Enum.ToObject(enumType, intVal), prop.PropertyType));
                                }
                            }

                            if (singleExpr != null)
                                combinedPropExpr = combinedPropExpr == null ? singleExpr : Expression.OrElse(combinedPropExpr, singleExpr);
                        }

                        if (combinedPropExpr != null)
                            query = query.Where(Expression.Lambda<Func<TEntity, bool>>(combinedPropExpr, parameter));
                    }
                }
            }

            // Search
            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                var search = request.SearchValue.ToLower();
                var parameter = Expression.Parameter(typeof(TEntity), "x");
                var stringProperties = typeof(TEntity).GetProperties().Where(p => p.PropertyType == typeof(string));

                if (!string.IsNullOrEmpty(request.SearchColumn) && !request.SearchColumn.Equals("all", StringComparison.OrdinalIgnoreCase))
                    stringProperties = stringProperties.Where(p => p.Name.Equals(request.SearchColumn, StringComparison.OrdinalIgnoreCase));

                Expression? combinedExpression = null;
                foreach (var property in stringProperties)
                {
                    var propertyAccess = Expression.Property(parameter, property);
                    var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
                    var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                    var notNull = Expression.NotEqual(propertyAccess, Expression.Constant(null));
                    var toLowerCall = Expression.Call(propertyAccess, toLowerMethod!);
                    var containsCall = Expression.Call(toLowerCall, containsMethod!, Expression.Constant(search));
                    var propertyExpression = Expression.AndAlso(notNull, containsCall);
                    combinedExpression = combinedExpression == null ? propertyExpression : Expression.OrElse(combinedExpression, propertyExpression);
                }

                if (combinedExpression != null)
                    query = query.Where(Expression.Lambda<Func<TEntity, bool>>(combinedExpression, parameter));
            }

            return await query
                .AsNoTracking()
                .Select(selector)
                .ToListAsync(cancellationToken);
        }
    }
}
