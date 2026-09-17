using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SSO.Application.Features.Tenants.Queries.GetByClientId
{
    public class GetTenantsByClientIdResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public bool Status { get; set; }

        [JsonPropertyName("role")]
        [Newtonsoft.Json.JsonProperty("role")]
        public string Role { get; set; } = "vendor";

        [JsonPropertyName("vendor_type")]
        [Newtonsoft.Json.JsonProperty("vendor_type")]
        public string? VendorType { get; set; }

        [JsonPropertyName("delivery_partner")]
        [Newtonsoft.Json.JsonProperty("delivery_partner")]
        public List<DeliveryPartnerResponse> DeliveryPartner { get; set; } = new List<DeliveryPartnerResponse>();
    }

    public class DeliveryPartnerResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; } = "delivery";
    }
}
