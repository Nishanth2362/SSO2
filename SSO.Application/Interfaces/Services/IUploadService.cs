using Microsoft.AspNetCore.Http;
using SSO.Application.Enums;
using SSO.Application.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSO.Application.Interfaces.Services
{
    public interface IUploadService
    {
        string UploadAsync(UploadRequest request);
        //Task<string> UploadAsync(IFormFile request, string fileName, UploadType uploadType);
    }
}
