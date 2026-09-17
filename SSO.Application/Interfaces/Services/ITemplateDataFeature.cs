using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Interfaces.Services
{
    public interface ITemplateDataFeature
    {
        Dictionary<string, string> Values { get; }
    }
}
