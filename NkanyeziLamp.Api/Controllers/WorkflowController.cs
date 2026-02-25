using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NkanyeziLamp.Api.Data;
using NkanyeziLamp.Api.Models;
using NkanyeziLamp.Api.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NkanyeziLamp.Api.Controllers
{
    [ApiController]
    [Route("api/workflow")]
    public class WorkflowController : ControllerBase
    {
        private readonly PdfExtractionService _pdfExtractionService;
        private readonly QuestionSplitterService _questionSplitterService;
        private readonly QuestionMemoMatcherService _matcherService;
        private readonly TeachingIntelligenceService _teachingService;
        private readonly BoardScriptService _boardScriptService;
        private readonly NarrationScriptService _narrationService;
        private readonly AudioGeneratorService _audioService;
        private readonly VideoRendererService _videoService;
        private readonly YouTubeOptimizationService _youtubeService;
        private readonly WorkflowStateStore _stateStore;
        private readonly LampDbContext _db;

        public WorkflowController(
            PdfExtractionService pdfExtractionService,
            QuestionSplitterService questionSplitterService,
            QuestionMemoMatcherService matcherService,
            TeachingIntelligenceService teachingService,
            BoardScriptService boardScriptService,
            NarrationScriptService narrationService,
            AudioGeneratorService audioService,
            VideoRendererService videoService,
            YouTubeOptimizationService youtubeService,
            WorkflowStateStore stateStore,
            LampDbContext db)
        {
            _pdfExtractionService = pdfExtractionService;
            _questionSplitterService = questionSplitterService;
            _matcherService = matcherService;
            _teachingService = teachingService;
            _boardScriptService = boardScriptService;
            _narrationService = narrationService;
            _audioService = audioService;
            _videoService = videoService;
            _youtubeService = youtubeService;
            _stateStore = stateStore;
            _db = db;
        }

        [HttpPost("upload/exam")]
        public async Task<IActionResult> UploadExam([FromForm] IFormFile questionPaper, [FromForm] string subject, [FromForm] string sessionId = null)
        {
            if (questionPaper == null || string.IsNullOrWhiteSpace(subject))
            {
                return BadRequest("Question paper and subject are required.");
            }

            var session = _stateStore.CreateOrGet(sessionId, subject);
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", subject, "Exam");
            Directory.CreateDirectory(uploadsFolder);

            var questionPath = Path.Combine(uploadsFolder, questionPaper.FileName);
            using (var stream = new FileStream(questionPath, FileMode.Create))
            {
                await questionPaper.CopyToAsync(stream);
            }

            var examPaper = new ExamPaper { Subject = subject, FilePath = questionPath };
            _db.ExamPapers.Add(examPaper);
            await _db.SaveChangesAsync();

            session.ExamPaperPath = questionPath;
            session.ExamPaperId = examPaper.Id;
            _stateStore.Save(session);

            return Ok(new { sessionId = session.SessionId, examPaperId = examPaper.Id, examPaperPath = questionPath });
        }

        [HttpPost("upload/memo")]
        public async Task<IActionResult> UploadMemo([FromForm] IFormFile memorandum, [FromForm] string subject, [FromForm] string sessionId)
        {
            if (memorandum == null || string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(sessionId))
            {
                return BadRequest("Memorandum, subject, and sessionId are required.");
            }

            var session = _stateStore.CreateOrGet(sessionId, subject);
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", subject, "Memo");
            Directory.CreateDirectory(uploadsFolder);

            var memoPath = Path.Combine(uploadsFolder, memorandum.FileName);
            using (var stream = new FileStream(memoPath, FileMode.Create))
            {
                await memorandum.CopyToAsync(stream);
            }

            var memo = new Memo { Subject = subject, FilePath = memoPath };
            _db.Memos.Add(memo);
            await _db.SaveChangesAsync();

            session.MemoPath = memoPath;
            session.MemoId = memo.Id;
            _stateStore.Save(session);

            return Ok(new { sessionId = session.SessionId, memoId = memo.Id, memoPath });
        }

        [HttpPost("upload/voice")]
        public async Task<IActionResult> UploadVoice([FromForm] IFormFile voiceOver, [FromForm] string sessionId)
        {
            if (voiceOver == null || string.IsNullOrWhiteSpace(sessionId))
            {
                return BadRequest("Voice-over file and sessionId are required.");
            }

            var session = _stateStore.Get(sessionId);
            if (session == null)
            {
                return NotFound("Session not found.");
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", session.Subject ?? "General", "Voice");
            Directory.CreateDirectory(uploadsFolder);

            var voicePath = Path.Combine(uploadsFolder, voiceOver.FileName);
            using (var stream = new FileStream(voicePath, FileMode.Create))
            {
                await voiceOver.CopyToAsync(stream);
            }

            session.VoiceOverPath = voicePath;
            _stateStore.Save(session);

            return Ok(new { sessionId, voiceOverPath = voicePath });
        }

        [HttpPost("upload/logo")]
        public async Task<IActionResult> UploadLogo([FromForm] IFormFile logo, [FromForm] string sessionId)
        {
            if (logo == null || string.IsNullOrWhiteSpace(sessionId))
            {
                return BadRequest("Logo file and sessionId are required.");
            }

            var session = _stateStore.Get(sessionId);
            if (session == null)
            {
                return NotFound("Session not found.");
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", session.Subject ?? "General", "Logo");
            Directory.CreateDirectory(uploadsFolder);

            var logoPath = Path.Combine(uploadsFolder, logo.FileName);
            using (var stream = new FileStream(logoPath, FileMode.Create))
            {
                await logo.CopyToAsync(stream);
            }

            session.Project ??= _boardScriptService.BuildProject(session.Subject, session.TeachingScripts);
            session.Project.Logo.Enabled = true;
            session.Project.Logo.LogoPath = logoPath;
            _stateStore.Save(session);
            SaveProjectToDisk(session);

            return Ok(new { sessionId, logoPath });
        }

        [HttpPost("process/{sessionId}/extract")]
        public async Task<IActionResult> ExtractAndMatch(string sessionId)
        {
            var session = _stateStore.Get(sessionId);
            if (session == null || string.IsNullOrWhiteSpace(session.ExamPaperPath) || string.IsNullOrWhiteSpace(session.MemoPath))
            {
                return BadRequest("Session must include both exam paper and memo.");
            }

            var (questionText, questionDiagrams) = await _pdfExtractionService.ExtractTextAndDiagramsAsync(session.ExamPaperPath);
            var (memoText, memoDiagrams) = await _pdfExtractionService.ExtractTextAndDiagramsAsync(session.MemoPath);

            session.ExamQuestions = _questionSplitterService.SplitQuestions(questionText);
            session.MemoAnswers = _questionSplitterService.SplitQuestions(memoText);

            for (int i = 0; i < session.ExamQuestions.Count; i++)
            {
                if (i < questionDiagrams.Count)
                {
                    session.ExamQuestions[i].DiagramPaths.Add(questionDiagrams[i]);
                }
            }

            session.Matches = _matcherService.MatchQuestions(session.ExamQuestions, session.MemoAnswers);

            await PersistExtractionAsync(session);
            _stateStore.Save(session);

            return Ok(new { sessionId, matches = session.Matches });
        }

        [HttpPut("process/{sessionId}/matches")]
        public IActionResult UpdateMatches(string sessionId, [FromBody] List<MatchedQuestion> matches)
        {
            var session = _stateStore.Get(sessionId);
            if (session == null)
            {
                return NotFound("Session not found.");
            }

            session.Matches = matches ?? new List<MatchedQuestion>();
            _stateStore.Save(session);
            return Ok(new { sessionId, matches = session.Matches });
        }

        [HttpPost("process/{sessionId}/scripts")]
        public async Task<IActionResult> GenerateScripts(string sessionId)
        {
            var session = _stateStore.Get(sessionId);
            if (session == null || session.Matches.Count == 0)
            {
                return BadRequest("No matched questions available.");
            }

            session.TeachingScripts = await _teachingService.GenerateTeachingScriptsAsync(session.Matches);
            await PersistScriptsAsync(session);
            _stateStore.Save(session);

            return Ok(new { sessionId, scripts = session.TeachingScripts });
        }

        [HttpPost("process/{sessionId}/project")]
        public async Task<IActionResult> BuildProject(string sessionId)
        {
            var session = _stateStore.Get(sessionId);
            if (session == null || session.TeachingScripts.Count == 0)
            {
                return BadRequest("Teaching scripts must be generated before creating a project.");
            }

            if (session.Narrations.Count == 0)
            {
                session.Narrations = _narrationService.GenerateNarrationScripts(session.Subject, session.TeachingScripts);
            }

            if (session.AudioFiles.Count == 0 && string.IsNullOrWhiteSpace(session.VoiceOverPath))
            {
                session.AudioFiles = await _audioService.GenerateAudioAsync(session.Narrations);
            }

            session.Project = _boardScriptService.BuildProject(session.Subject, session.TeachingScripts);
            SaveProjectToDisk(session);
            _stateStore.Save(session);

            return Ok(new { sessionId, project = session.Project });
        }

        [HttpPut("process/{sessionId}/project")]
        public IActionResult UpdateProject(string sessionId, [FromBody] VideoProject project)
        {
            var session = _stateStore.Get(sessionId);
            if (session == null)
            {
                return NotFound("Session not found.");
            }

            session.Project = MergeProject(session.Project ?? _boardScriptService.BuildProject(session.Subject, session.TeachingScripts), project);
            _boardScriptService.ApplyIntroOutroEdits(session.Project);
            SaveProjectToDisk(session);
            _stateStore.Save(session);

            return Ok(new { sessionId, project = session.Project });
        }

        [HttpPut("process/{sessionId}/scripts")]
        public IActionResult UpdateScripts(string sessionId, [FromBody] List<UpdateScriptRequest> scripts)
        {
            var session = _stateStore.Get(sessionId);
            if (session == null)
            {
                return NotFound("Session not found.");
            }

            foreach (var update in scripts ?? new List<UpdateScriptRequest>())
            {
                var existing = session.TeachingScripts.FirstOrDefault(s => s.QuestionNumber == update.QuestionNumber);
                if (existing != null)
                {
                    existing.DraftScript = update.DraftScript ?? existing.DraftScript;
                    existing.CommonMistakes = update.CommonMistakes ?? existing.CommonMistakes;
                    existing.MarksBreakdown = update.MarksBreakdown ?? existing.MarksBreakdown;
                }
            }

            _stateStore.Save(session);
            return Ok(new { sessionId, scripts = session.TeachingScripts });
        }

        [HttpPost("process/{sessionId}/scripts/regenerate/{questionNumber}")]
        public async Task<IActionResult> RegenerateScript(string sessionId, string questionNumber)
        {
            var session = _stateStore.Get(sessionId);
            if (session == null)
            {
                return NotFound("Session not found.");
            }

            var match = session.Matches.FirstOrDefault(m => m.QuestionNumber == questionNumber);
            if (match == null)
            {
                return NotFound("Matched question not found.");
            }

            var regenerated = await _teachingService.GenerateTeachingScriptsAsync(new List<MatchedQuestion> { match });
            var script = regenerated.FirstOrDefault();
            if (script != null)
            {
                var existingIndex = session.TeachingScripts.FindIndex(s => s.QuestionNumber == questionNumber);
                if (existingIndex >= 0)
                {
                    session.TeachingScripts[existingIndex] = script;
                }
                else
                {
                    session.TeachingScripts.Add(script);
                }
            }

            _stateStore.Save(session);
            return Ok(new { sessionId, script });
        }

        [HttpPost("process/{sessionId}/render")]
        public async Task<IActionResult> RenderVideo(string sessionId)
        {
            var session = _stateStore.Get(sessionId);
            if (session == null || session.TeachingScripts.Count == 0)
            {
                return BadRequest("Teaching scripts must be generated before rendering.");
            }

            if (session.Project == null)
            {
                return BadRequest("Video project must be created before rendering.");
            }

            var projectBlocks = BuildWritingBlocks(session);
            var projectAudio = BuildAudioFromProject(session);
            if (!string.IsNullOrWhiteSpace(session.VoiceOverPath))
            {
                projectAudio = new List<WorkflowAudioFile>
                {
                    new WorkflowAudioFile { QuestionNumber = "voiceover", AudioPath = session.VoiceOverPath }
                };
            }
            session.VideoPath = await _videoService.RenderBoardVideoAsync(session.Project, projectBlocks, projectAudio);

            var summary = session.TeachingScripts.FirstOrDefault()?.TeachingExplanation ?? "CAPS-aligned solutions with step-by-step explanations.";
            session.YouTubeMeta = _youtubeService.GenerateMeta(session.Subject, "Exam Solutions", summary);

            await PersistRenderArtifactsAsync(session);
            await PersistVideoAsync(session);
            _stateStore.Save(session);

            return Ok(new
            {
                sessionId,
                videoPath = session.VideoPath,
                youtubeMeta = session.YouTubeMeta
            });
        }

        [HttpGet("process/{sessionId}")]
        public IActionResult GetSession(string sessionId)
        {
            var session = _stateStore.Get(sessionId);
            if (session == null)
            {
                return NotFound("Session not found.");
            }

            return Ok(session);
        }

        [HttpGet("video/{sessionId}")]
        public IActionResult GetVideo(string sessionId)
        {
            var session = _stateStore.Get(sessionId);
            if (session == null || string.IsNullOrWhiteSpace(session.VideoPath) || !System.IO.File.Exists(session.VideoPath))
            {
                return NotFound("Video not found.");
            }

            var stream = new FileStream(session.VideoPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return File(stream, "video/mp4", "NkanyeziLamp.mp4");
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var history = await _db.VideoFiles
                .OrderByDescending(v => v.Id)
                .Select(v => new
                {
                    v.Id,
                    v.Subject,
                    v.VideoPath,
                    v.Title,
                    v.Description,
                    v.Tags,
                    v.ThumbnailText
                })
                .ToListAsync();

            return Ok(history);
        }

        [HttpPost("youtube-meta")]
        public IActionResult GenerateYouTubeMeta([FromBody] Dictionary<string, string> payload)
        {
            var subject = payload.TryGetValue("subject", out var s) ? s : "Subject";
            var examTitle = payload.TryGetValue("examTitle", out var e) ? e : "Exam Solutions";
            var summary = payload.TryGetValue("summary", out var t) ? t : "CAPS-aligned solutions with step-by-step explanations.";
            var meta = _youtubeService.GenerateMeta(subject, examTitle, summary);
            return Ok(meta);
        }

        private async Task PersistExtractionAsync(WorkflowSessionState session)
        {
            if (session.ExamPaperId.HasValue)
            {
                foreach (var q in session.ExamQuestions)
                {
                    _db.ExamQuestions.Add(new ExamQuestion
                    {
                        ExamPaperId = session.ExamPaperId.Value,
                        QuestionNumber = q.QuestionNumber,
                        QuestionText = q.QuestionText
                    });
                }
            }

            if (session.MemoId.HasValue)
            {
                foreach (var m in session.MemoAnswers)
                {
                    _db.MemoAnswers.Add(new MemoAnswer
                    {
                        MemoId = session.MemoId.Value,
                        QuestionNumber = m.QuestionNumber,
                        AnswerText = m.QuestionText
                    });
                }
            }

            await _db.SaveChangesAsync();
        }

        private async Task PersistScriptsAsync(WorkflowSessionState session)
        {
            var scriptEntities = new List<TeachingScript>();
            foreach (var script in session.TeachingScripts)
            {
                var entity = new TeachingScript
                {
                    QuestionNumber = script.QuestionNumber,
                    FullExplanation = script.DraftScript ?? string.Empty,
                    TeachingNotes = $"{script.CommonMistakes}\n{script.MarksBreakdown}"
                };
                scriptEntities.Add(entity);
                _db.TeachingScripts.Add(entity);
            }

            await _db.SaveChangesAsync();

            for (int i = 0; i < session.TeachingScripts.Count; i++)
            {
                session.TeachingScripts[i].TeachingScriptId = scriptEntities[i].Id;
            }
        }

        private async Task PersistVideoAsync(WorkflowSessionState session)
        {
            if (string.IsNullOrWhiteSpace(session.VideoPath))
            {
                return;
            }

            _db.VideoFiles.Add(new VideoFile
            {
                Subject = session.Subject,
                VideoPath = session.VideoPath,
                Title = session.YouTubeMeta?.Title,
                Description = session.YouTubeMeta?.Description,
                Tags = session.YouTubeMeta?.Tags,
                ThumbnailText = session.YouTubeMeta?.ThumbnailText
            });

            await _db.SaveChangesAsync();
        }

        private async Task PersistRenderArtifactsAsync(WorkflowSessionState session)
        {
            var defaultTeachingScriptId = session.TeachingScripts.FirstOrDefault()?.TeachingScriptId ?? 0;

            foreach (var slide in session.Slides)
            {
                _db.Slides.Add(new Slide
                {
                    Title = slide.Title ?? string.Empty,
                    Content = slide.Content ?? string.Empty,
                    DiagramPath = slide.DiagramPath ?? string.Empty,
                    TeachingScriptId = defaultTeachingScriptId
                });
            }

            foreach (var narration in session.Narrations)
            {
                _db.NarrationScripts.Add(new NarrationScript
                {
                    QuestionNumber = narration.QuestionNumber,
                    VoiceText = narration.VoiceText ?? string.Empty,
                    TeachingScriptId = defaultTeachingScriptId
                });
            }

            foreach (var audio in session.AudioFiles)
            {
                _db.AudioFiles.Add(new AudioFile
                {
                    QuestionNumber = audio.QuestionNumber,
                    AudioPath = audio.AudioPath ?? string.Empty,
                    NarrationScriptId = 0
                });
            }

            await _db.SaveChangesAsync();
        }

        private static VideoProject MergeProject(VideoProject existing, VideoProject updates)
        {
            if (updates == null)
            {
                return existing;
            }

            existing.IntroText = string.IsNullOrWhiteSpace(updates.IntroText) ? existing.IntroText : updates.IntroText;
            existing.OutroText = string.IsNullOrWhiteSpace(updates.OutroText) ? existing.OutroText : updates.OutroText;

            if (updates.Logo != null)
            {
                existing.Logo.Enabled = updates.Logo.Enabled;
                if (!string.IsNullOrWhiteSpace(updates.Logo.LogoPath))
                {
                    existing.Logo.LogoPath = updates.Logo.LogoPath;
                }
                if (!string.IsNullOrWhiteSpace(updates.Logo.Position))
                {
                    existing.Logo.Position = updates.Logo.Position;
                }
                if (updates.Logo.SizePercent > 0)
                {
                    existing.Logo.SizePercent = updates.Logo.SizePercent;
                }
            }

            if (updates.Acknowledgment != null)
            {
                existing.Acknowledgment.Enabled = updates.Acknowledgment.Enabled;
                existing.Acknowledgment.Text = updates.Acknowledgment.Text;
                if (!string.IsNullOrWhiteSpace(updates.Acknowledgment.Placement))
                {
                    existing.Acknowledgment.Placement = updates.Acknowledgment.Placement;
                }
            }

            return existing;
        }

        private static List<WritingBlock> BuildWritingBlocks(WorkflowSessionState session)
        {
            var blocks = new List<WritingBlock>();
            if (session?.Project?.Scenes == null)
            {
                return blocks;
            }

            var cursor = 0.0;
            foreach (var scene in session.Project.Scenes)
            {
                var sceneBlocks = scene.WritingBlocks ?? new List<WritingBlock>();
                foreach (var block in sceneBlocks)
                {
                    blocks.Add(new WritingBlock
                    {
                        Text = block.Text,
                        StartSeconds = block.StartSeconds + cursor,
                        DurationSeconds = block.DurationSeconds,
                        X = block.X,
                        Y = block.Y,
                        FontSize = block.FontSize,
                        Color = block.Color,
                        Highlight = block.Highlight
                    });
                }
                cursor += scene.DurationSeconds;
            }

            if (session.Project.Acknowledgment.Enabled)
            {
                var ackText = string.IsNullOrWhiteSpace(session.Project.Acknowledgment.Text)
                    ? "Special thanks to...\nExam Source...\nNkanyezi Lamp"
                    : session.Project.Acknowledgment.Text;

                var ackBlock = new WritingBlock
                {
                    Text = ackText,
                    StartSeconds = session.Project.Acknowledgment.Placement == "start" ? 0 : cursor,
                    DurationSeconds = 6,
                    X = 120,
                    Y = 160,
                    FontSize = 40,
                    Color = "white",
                    Highlight = false
                };
                if (session.Project.Acknowledgment.Placement == "start")
                {
                    foreach (var block in blocks)
                    {
                        block.StartSeconds += ackBlock.DurationSeconds;
                    }
                    blocks.Insert(0, ackBlock);
                }
                else
                {
                    blocks.Add(ackBlock);
                    cursor += ackBlock.DurationSeconds;
                }
            }

            return blocks;
        }

        private static List<WorkflowAudioFile> BuildAudioFromProject(WorkflowSessionState session)
        {
            var audioFiles = new List<WorkflowAudioFile>();
            foreach (var scene in session.Project.Scenes.Where(s => s.Type == "question"))
            {
                if (!string.IsNullOrWhiteSpace(scene.AudioPath))
                {
                    audioFiles.Add(new WorkflowAudioFile { QuestionNumber = scene.QuestionNumber, AudioPath = scene.AudioPath });
                }
            }
            return audioFiles;
        }

        private static void SaveProjectToDisk(WorkflowSessionState session)
        {
            if (session.Project == null)
            {
                return;
            }

            var projectDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Projects");
            Directory.CreateDirectory(projectDir);
            var path = Path.Combine(projectDir, $"{session.SessionId}.json");
            var json = JsonSerializer.Serialize(session.Project, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(path, json);
            session.ProjectPath = path;
        }
    }
}
