using System;
using System.Collections.Generic;

namespace NkanyeziLamp.Api.Models
{
    public class WorkflowSessionState
    {
        public string SessionId { get; set; }
        public string Subject { get; set; }
        public string ExamPaperPath { get; set; }
        public string MemoPath { get; set; }
        public int? ExamPaperId { get; set; }
        public int? MemoId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public List<ExtractedQuestion> ExamQuestions { get; set; } = new();
        public List<ExtractedQuestion> MemoAnswers { get; set; } = new();
        public List<MatchedQuestion> Matches { get; set; } = new();
        public List<WorkflowTeachingScript> TeachingScripts { get; set; } = new();
        public List<WorkflowSlide> Slides { get; set; } = new();
        public List<WorkflowNarrationScript> Narrations { get; set; } = new();
        public List<WorkflowAudioFile> AudioFiles { get; set; } = new();
        public string VoiceOverPath { get; set; }
        public VideoProject Project { get; set; }
        public string ProjectPath { get; set; }
        public string VideoPath { get; set; }
        public YouTubeMetaResponse YouTubeMeta { get; set; }
    }

    public class ExtractedQuestion
    {
        public string QuestionNumber { get; set; }
        public string QuestionText { get; set; }
        public int? Marks { get; set; }
        public List<string> DiagramPaths { get; set; } = new();
    }

    public class MatchedQuestion
    {
        public string QuestionNumber { get; set; }
        public string QuestionText { get; set; }
        public string AnswerText { get; set; }
        public int? Marks { get; set; }
        public bool NeedsReview { get; set; }
        public double SimilarityScore { get; set; }
        public string MatchReason { get; set; }
    }

    public class WorkflowTeachingScript
    {
        public string QuestionNumber { get; set; }
        public int? TeachingScriptId { get; set; }
        public string RestatedQuestion { get; set; }
        public List<string> Steps { get; set; } = new();
        public string TeachingExplanation { get; set; }
        public string CommonMistakes { get; set; }
        public string MarksBreakdown { get; set; }
        public string DraftScript { get; set; }
    }

    public class WorkflowSlide
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string DiagramPath { get; set; }
    }

    public class WorkflowNarrationScript
    {
        public string QuestionNumber { get; set; }
        public string VoiceText { get; set; }
    }

    public class WorkflowAudioFile
    {
        public string QuestionNumber { get; set; }
        public string AudioPath { get; set; }
    }

    public class YouTubeMetaResponse
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Hashtags { get; set; }
        public string Tags { get; set; }
        public string ThumbnailText { get; set; }
    }

    public class UpdateScriptRequest
    {
        public string QuestionNumber { get; set; }
        public string DraftScript { get; set; }
        public string CommonMistakes { get; set; }
        public string MarksBreakdown { get; set; }
    }

    public class VideoProject
    {
        public string Subject { get; set; }
        public string IntroText { get; set; }
        public string OutroText { get; set; }
        public LogoSettings Logo { get; set; } = new();
        public AcknowledgmentSettings Acknowledgment { get; set; } = new();
        public List<VideoScene> Scenes { get; set; } = new();
    }

    public class VideoScene
    {
        public string Type { get; set; }
        public string QuestionNumber { get; set; }
        public double DurationSeconds { get; set; }
        public List<WorkflowSlide> Slides { get; set; } = new();
        public List<WritingBlock> WritingBlocks { get; set; } = new();
        public string AudioPath { get; set; }
    }

    public class WritingBlock
    {
        public string Text { get; set; }
        public double StartSeconds { get; set; }
        public double DurationSeconds { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int FontSize { get; set; } = 40;
        public string Color { get; set; } = "white";
        public bool Highlight { get; set; }
    }

    public class LogoSettings
    {
        public bool Enabled { get; set; }
        public string LogoPath { get; set; }
        public string Position { get; set; } = "top-right";
        public int SizePercent { get; set; } = 12;
    }

    public class AcknowledgmentSettings
    {
        public bool Enabled { get; set; }
        public string Text { get; set; }
        public string Placement { get; set; } = "end";
    }
}
