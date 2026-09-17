using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;

namespace SSO.Application.Extensions
{
    public static class EnumExtensions
    {
        public static async Task<T> ToResult<T>(this HttpResponseMessage response)
        {
            try
            {
                var responseAsString = await response.Content.ReadAsStringAsync();
                var responseObject = JsonSerializer.Deserialize<T>(responseAsString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return responseObject!;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        public static T ToEnum<T>(this string value, bool ignoreCase = true)
        {
            return (T)Enum.Parse(typeof(T), value, ignoreCase);
        }
        public static string ToDescriptionString(this Enum val)
        {
            DescriptionAttribute[] attributes = (DescriptionAttribute[])val.GetType().GetField(val.ToString())!.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return attributes?.Length > 0
                ? attributes[0].Description
                : val.ToString();
        }

        public static bool RequiresClientSecret(this SSO.Domain.Enums.ClientType clientType)
        {
            return clientType == SSO.Domain.Enums.ClientType.Web || clientType == SSO.Domain.Enums.ClientType.Machine;
        }
    }
}
