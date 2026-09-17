using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Responses.DataTable
{
    public class DataTableResponse<T>
    {
        public int Draw { get; set; }
        public int RecordsTotal { get; set; }
        public int RecordsFiltered { get; set; }
        public IEnumerable<T> Data { get; set; } = new List<T>();
    }
}
