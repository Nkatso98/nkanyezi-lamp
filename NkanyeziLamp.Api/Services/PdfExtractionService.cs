using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Microsoft.Extensions.Options;
using NkanyeziLamp.Api.Models;
using Tesseract;
using Tesseract.Interop;

namespace NkanyeziLamp.Api.Services
{
    public class PdfExtractionService
    {
        private readonly OcrOptions _options;

        public PdfExtractionService(IOptions<OcrOptions> options)
        {
            _options = options.Value ?? new OcrOptions();
        }

        public async Task<(string text, List<string> diagrams)> ExtractTextAndDiagramsAsync(string pdfPath)
        {
            var sb = new StringBuilder();
            var diagrams = new List<string>();
            var tessdataPath = ResolveTessdataPath();
            // Use PdfiumViewer for PDF rendering (NuGet: PdfiumViewer)
            using (var document = PdfiumViewer.PdfDocument.Load(pdfPath))
            {
                for (int i = 0; i < document.PageCount; i++)
                {
                    // Try to extract selectable text
                    var text = document.GetPdfText(i);
                    using (var image = document.Render(i, 300, 300, true))
                    {
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            sb.AppendLine(text);
                        }
                        else
                        {
                            // If no selectable text, use OCR
                            using (var engine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default))
                            using (var bmp = new Bitmap(image))
                            using (var pix = PixConverter.ToPix(bmp))
                            {
                                var ocrResult = engine.Process(pix);
                                sb.AppendLine(ocrResult.GetText());
                            }
                        }

                        // Save rendered page as diagram reference
                        var diagramPath = Path.Combine("Uploads", "Diagrams", $"{Path.GetFileNameWithoutExtension(pdfPath)}_page{i + 1}.png");
                        Directory.CreateDirectory(Path.GetDirectoryName(diagramPath));
                        image.Save(diagramPath);
                        diagrams.Add(diagramPath);
                    }
                }
            }
            return (sb.ToString(), diagrams);
        }

        private string ResolveTessdataPath()
        {
            if (!string.IsNullOrWhiteSpace(_options.TessdataPath))
            {
                return _options.TessdataPath;
            }

            var env = Environment.GetEnvironmentVariable("TESSDATA_PREFIX");
            if (!string.IsNullOrWhiteSpace(env))
            {
                return env.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            return "./tessdata";
        }
    }
}
