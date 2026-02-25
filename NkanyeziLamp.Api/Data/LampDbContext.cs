using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace NkanyeziLamp.Api.Data
{
    public class LampDbContext : DbContext
    {
        public LampDbContext(DbContextOptions<LampDbContext> options) : base(options) { }
        public DbSet<ExamPaper> ExamPapers { get; set; }
        public DbSet<Memo> Memos { get; set; }
        public DbSet<ExamQuestion> ExamQuestions { get; set; }
        public DbSet<MemoAnswer> MemoAnswers { get; set; }
        public DbSet<TeachingScript> TeachingScripts { get; set; }
        public DbSet<Slide> Slides { get; set; }
        public DbSet<NarrationScript> NarrationScripts { get; set; }
        public DbSet<AudioFile> AudioFiles { get; set; }
        public DbSet<VideoFile> VideoFiles { get; set; }
    }

    public class ExamPaper { public int Id { get; set; } public string Subject { get; set; } public string FilePath { get; set; } }
    public class Memo { public int Id { get; set; } public string Subject { get; set; } public string FilePath { get; set; } }
    public class ExamQuestion { public int Id { get; set; } public string QuestionNumber { get; set; } public string QuestionText { get; set; } public int ExamPaperId { get; set; } }
    public class MemoAnswer { public int Id { get; set; } public string QuestionNumber { get; set; } public string AnswerText { get; set; } public int MemoId { get; set; } }
    public class TeachingScript { public int Id { get; set; } public string QuestionNumber { get; set; } public string FullExplanation { get; set; } public string TeachingNotes { get; set; } public int ExamQuestionId { get; set; } }
    public class Slide { public int Id { get; set; } public string Title { get; set; } public string Content { get; set; } public string DiagramPath { get; set; } public int TeachingScriptId { get; set; } }
    public class NarrationScript { public int Id { get; set; } public string QuestionNumber { get; set; } public string VoiceText { get; set; } public int TeachingScriptId { get; set; } }
    public class AudioFile { public int Id { get; set; } public string QuestionNumber { get; set; } public string AudioPath { get; set; } public int NarrationScriptId { get; set; } }
    public class VideoFile { public int Id { get; set; } public string Subject { get; set; } public string VideoPath { get; set; } public string Title { get; set; } public string Description { get; set; } public string Tags { get; set; } public string ThumbnailText { get; set; } }
}
