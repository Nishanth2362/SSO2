using Microsoft.AspNetCore.Http;
using SSO.Application.Requests.DataTable;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Extensions
{
    public static class DataTableExtensions
    {
        public static DataTableRequest ToDataTableRequest(this HttpRequest request)
        {
            IFormCollection? form = null;
            if (request.HasFormContentType)
            {
                try
                {
                    form = request.Form;
                }
                catch (Exception)
                {
                    // Ignore client reset / connection abort exception
                }
            }

            var drawStr = form != null ? form["draw"].FirstOrDefault() : request.Query["draw"].FirstOrDefault();
            var startStr = form != null ? form["start"].FirstOrDefault() : request.Query["start"].FirstOrDefault();
            var lengthStr = form != null ? form["length"].FirstOrDefault() : request.Query["length"].FirstOrDefault();
            var searchValue = form != null ? form["search[value]"].FirstOrDefault() : request.Query["search[value]"].FirstOrDefault();

            var sortColumnIndex = form != null ? form["order[0][column]"].FirstOrDefault() : request.Query["order[0][column]"].FirstOrDefault();
            var sortColumn = form != null ? form[$"columns[{sortColumnIndex}][data]"].FirstOrDefault() : request.Query[$"columns[{sortColumnIndex}][data]"].FirstOrDefault();
            var sortDirection = form != null ? form["order[0][dir]"].FirstOrDefault() : request.Query["order[0][dir]"].FirstOrDefault();

            var startDateStr = form != null ? form["startDate"].FirstOrDefault() : request.Query["startDate"].FirstOrDefault();
            var endDateStr = form != null ? form["endDate"].FirstOrDefault() : request.Query["endDate"].FirstOrDefault();

            DateTime? startDate = null;
            DateTime? endDate = null;

            if (DateTime.TryParse(startDateStr, out var sd)) startDate = sd;
            if (DateTime.TryParse(endDateStr, out var ed)) endDate = ed;

            var searchColumn = form != null ? form["searchColumn"].FirstOrDefault() : request.Query["searchColumn"].FirstOrDefault();

            var filters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (form != null)
            {
                foreach (var key in form.Keys)
                {
                    if (key.StartsWith("filter_", StringComparison.OrdinalIgnoreCase))
                    {
                        var values = form[key].Where(v => !string.IsNullOrEmpty(v)).ToList();
                        if (values.Any())
                        {
                            var filterName = key.Substring("filter_".Length);
                            if (filterName.EndsWith("[]"))
                            {
                                filterName = filterName.Substring(0, filterName.Length - 2);
                            }
                            filters[filterName] = string.Join(",", values);
                        }
                    }
                }
            }
            else
            {
                foreach (var key in request.Query.Keys)
                {
                    if (key.StartsWith("filter_", StringComparison.OrdinalIgnoreCase))
                    {
                        var values = request.Query[key].Where(v => !string.IsNullOrEmpty(v)).ToList();
                        if (values.Any())
                        {
                            var filterName = key.Substring("filter_".Length);
                            if (filterName.EndsWith("[]"))
                            {
                                filterName = filterName.Substring(0, filterName.Length - 2);
                            }
                            filters[filterName] = string.Join(",", values);
                        }
                    }
                }
            }

            int.TryParse(drawStr, out var draw);
            int.TryParse(startStr, out var start);
            if (!int.TryParse(lengthStr, out var length))
            {
                length = 10;
            }

            return new DataTableRequest
            {
                Draw = draw,
                Start = start,
                Length = length,
                SearchValue = searchValue,
                SortColumn = sortColumn,
                SortDirection = sortDirection,
                SearchColumn = searchColumn,
                Filters = filters,
                StartDate = startDate,
                EndDate = endDate
            };
        }
    }
}
