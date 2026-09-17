using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSO.Application.Enums
{
    public enum UploadType : byte
    {

        [Description(@"Images\ProfilePictures")]
        ProfilePicture,
        [Description(@"Documents")]
        Document,
        [Description(@"Images\TenantLogos")]
        TenantLogo,
        [Description(@"Images\TenantFavicons")]
        TenantFavicon
    }
}
