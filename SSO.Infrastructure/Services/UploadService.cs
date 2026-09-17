using Microsoft.Extensions.Logging;
using SSO.Application.Enums;
using SSO.Application.Extensions;
using SSO.Application.Interfaces.Services;
using SSO.Application.Requests;

namespace SSO.Infrastructure.Services
{
    public class UploadService : IUploadService
    {
        private const int TenantImageMaxBytes = 2 * 1024 * 1024;
        private const int DocumentMaxBytes = 15 * 1024 * 1024;
        private readonly ILogger<UploadService> _logger;

        public UploadService(ILogger<UploadService> logger)
        {
            _logger = logger;
        }

        public string UploadAsync(UploadRequest request)
        {
            if (request?.Data == null || request.Data.Length == 0)
            {
                _logger.LogWarning("Upload rejected because the payload was empty.");
                return string.Empty;
            }

            if (!IsAllowedUpload(request, out var extension))
            {
                _logger.LogWarning("Upload rejected for {UploadType} with file name {FileName}.", request.UploadType, request.FileName);
                return string.Empty;
            }

            var folder = request.UploadType.ToDescriptionString();
            var folderName = Path.Combine("Files", folder);
            var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);
            Directory.CreateDirectory(pathToSave);

            var generatedFileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(pathToSave, generatedFileName);
            var dbPath = Path.Combine(folderName, generatedFileName);

            using (var streamData = new MemoryStream(request.Data, writable: false))
            using (var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                streamData.CopyTo(stream);
            }

            return dbPath;
        }

        private static bool IsAllowedUpload(UploadRequest request, out string extension)
        {
            extension = Path.GetExtension(request.FileName ?? string.Empty).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(extension))
            {
                return false;
            }

            if (request.Data.Length > TenantImageMaxBytes)
            {
                if (request.UploadType is UploadType.TenantLogo or UploadType.TenantFavicon)
                {
                    return false;
                }
            }

            if (request.UploadType == UploadType.Document && request.Data.Length > DocumentMaxBytes)
            {
                return false;
            }

            return request.UploadType switch
            {
                UploadType.TenantLogo or UploadType.TenantFavicon =>
                    extension is ".png" or ".jpg" or ".jpeg" or ".webp" or ".ico",
                UploadType.Document =>
                    extension is ".xlsx" or ".xls" or ".csv",
                _ => false
            };
        }
    }
}
