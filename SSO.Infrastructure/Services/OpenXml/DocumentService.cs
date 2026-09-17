using DocumentFormat.OpenXml.Features;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SSO.Application.Helper;
using SSO.Application.Interfaces.Services;
using SSO.Domain.Enums;

namespace SSO.Infrastructure.Services.OpenXml
{
    public sealed class DocumentService : IDocumentService
    {
        private readonly ITemplateResolver _resolver;
        private readonly ILogger<DocumentService> _logger;
        public DocumentService(ITemplateResolver resolver,ILogger<DocumentService> logger)
        {
            _resolver = resolver;
            _logger = logger;
        }
        
        public async Task<byte[]> ExportAsync<TModel>(string templateKey, TModel model, CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Exporting document with templateKey: {TemplateKey}", templateKey);  
                // 1️⃣ Load template from DB
                var template = await _resolver.ResolveAsync(templateKey, ct);

                using var stream = new MemoryStream(await File.ReadAllBytesAsync(template.Content));

                OpenXmlPackage document;
                switch (template.Type)
                {
                    case DocumentType.Excel:
                        document = SpreadsheetDocument.Open(stream, true);
                        break;
                    case DocumentType.Word:
                        document = WordprocessingDocument.Open(stream, true);
                        break;
                    case DocumentType.PowerPoint:
                        document = PresentationDocument.Open(stream, true);
                        break;
                    default: throw new NotSupportedException();
                }
                ;

                // 2️⃣ Scan for tokens
                var tokens = OpenXmlTokenScanner.Scan(document);

                // 3️⃣ Validate tokens exist in model
                var missing = new TokenValidator().Validate(tokens, model);
                if (missing.Any())
                    throw new InvalidOperationException(
                        $"Missing tokens in model: {string.Join(", ", missing)}");

                // 4️⃣ Build feature
                var featureCollection = new FeatureCollection();
                var feature = new TemplateDataFeature();
                foreach (var prop in typeof(TModel).GetProperties())
                {
                    feature.Values[prop.Name] = prop.GetValue(model)?.ToString() ?? string.Empty;
                }

                featureCollection.Set<ITemplateDataFeature>(feature);

                // 5️⃣ Replace tokens
                OpenXmlTokenReplacer.Apply(document, feature.Values);

                document.Save();
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting document with templateKey: {TemplateKey}", templateKey);
                return Array.Empty<byte>();
            }
           
        }
    }
}
