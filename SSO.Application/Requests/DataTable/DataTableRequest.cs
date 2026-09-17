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
    }
}
