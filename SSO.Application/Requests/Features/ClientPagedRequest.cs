using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Requests.Features
{
    public class ClientPagedRequest: PagedRequest
    {
        public Guid TenantId { get; set; }
        public string SearchData { get; set; }
    }
}
