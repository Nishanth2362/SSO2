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
            var draw = request.Form["draw"].FirstOrDefault();
            var start = request.Form["start"].FirstOrDefault();
            var length = request.Form["length"].FirstOrDefault();
            var searchValue = request.Form["search[value]"].FirstOrDefault();

            var sortColumnIndex = request.Form["order[0][column]"].FirstOrDefault();
            var sortColumn = request.Form[$"columns[{sortColumnIndex}][data]"].FirstOrDefault();
            var sortDirection = request.Form["order[0][dir]"].FirstOrDefault();

            return new DataTableRequest
            {
                Draw = Convert.ToInt32(draw),
                Start = Convert.ToInt32(start),
                Length = Convert.ToInt32(length),
                SearchValue = searchValue,
                SortColumn = sortColumn,
                SortDirection = sortDirection
            };
        }
    }
}
