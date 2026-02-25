using NkanyeziLamp.Api.Models;
using NkanyeziLamp.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace NkanyeziLamp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly PdfExtractionService _pdfExtractionService = new PdfExtractionService();
        private readonly QuestionSplitterService _questionSplitterService = new QuestionSplitterService();
        private readonly QuestionMemoMatcherService _matcherService = new QuestionMemoMatcherService();
        private readonly TeachingIntelligenceService _teachingService = new TeachingIntelligenceService();
        private readonly SlideGeneratorService _slideService = new SlideGeneratorService();
        private readonly NarrationScriptService _narrationService = new NarrationScriptService();
        private readonly AudioGeneratorService _audioService = new AudioGeneratorService();
        private readonly VideoRendererService _videoService = new VideoRendererService();

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFiles([FromForm] IFormFile questionPaper, [FromForm] IFormFile memorandum, [FromForm] string subject)
        {
            if (questionPaper == null || memorandum == null || string.IsNullOrEmpty(subject))
                return BadRequest("Both files and subject are required.");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", subject);
            Directory.CreateDirectory(uploadsFolder);

            var questionPath = Path.Combine(uploadsFolder, questionPaper.FileName);
            var memoPath = Path.Combine(uploadsFolder, memorandum.FileName);

            using (var stream = new FileStream(questionPath, FileMode.Create))
            {
                await questionPaper.CopyToAsync(stream);
            }
            using (var stream = new FileStream(memoPath, FileMode.Create))
            {
                await memorandum.CopyToAsync(stream);
            }

            // Extract text from PDFs
                var (questionText, questionDiagrams) = await _pdfExtractionService.ExtractTextAndDiagramsAsync(questionPath);
                var (memoText, memoDiagrams) = await _pdfExtractionService.ExtractTextAndDiagramsAsync(memoPath);

            // Split into questions and memo answers
            var examQuestions = _questionSplitterService.SplitQuestions(questionText);
            var memoAnswers = _questionSplitterService.SplitQuestions(memoText);

            // Match questions to memo answers
            var matchedQuestions = _matcherService.MatchQuestions(examQuestions, memoAnswers);

            // Generate teaching scripts (AI step)
            var teachingScripts = await _teachingService.GenerateTeachingScriptsAsync(matchedQuestions);

            // Generate slides
            var slides = _slideService.GenerateSlides(subject, teachingScripts);

            // Generate narration scripts
            var narrationScripts = _narrationService.GenerateNarrationScripts(subject, teachingScripts);

            // Generate audio files
            var audioFiles = await _audioService.GenerateAudioAsync(narrationScripts);

            // Render video
            var videoPath = await _videoService.RenderVideoAsync(slides, audioFiles, null);

            // TODO: Save video metadata to database

            return Ok(new { questionPath, memoPath, matchedQuestions, teachingScripts, slides, narrationScripts, audioFiles, videoPath });
        }
    }
}
