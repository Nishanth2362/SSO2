using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Requests.DataTable
{
    public class DataTableRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public string? SearchValue { get; set; }
        public string? SortColumn { get; set; }
        public string? SortDirection { get; set; }
        public string? SearchColumn { get; set; }
        public Dictionary<string, string>? Filters { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        /// <summary>
        /// When set, the export will be restricted to only these record IDs.
        /// Takes priority over filters and search for export operations.
        /// </summary>
        public List<string>? SelectedIds { get; set; }
    }
}
