using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Configuaration
{
    public class AppConfiguration
    {
        public string Secret { get; set; }

        public bool BehindSSLProxy { get; set; }

        public string ProxyIP { get; set; }

        public string ApplicationUrl { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public string CertificateType { get; set; }
        public string EncryptionAlgorithm { get; set; }
    }
}
