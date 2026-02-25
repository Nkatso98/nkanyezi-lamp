using System.Collections.Generic;
using NkanyeziLamp.Api.Models;

namespace NkanyeziLamp.Api.Services
{
    public class YouTubeOptimizationService
    {
        public YouTubeMetaResponse GenerateMeta(string subject, string examTitle, string teachingSummary)
        {
            // Example teacher-style meta generation
            var title = $"{subject} | {examTitle} | Full Paper Solutions";
            var description = $"In this lesson, we solve {examTitle} for {subject}. {teachingSummary}\n\nThis video is part of the Nkanyezi Lamp CAPS Exam-to-Video series. Subscribe for more lessons!";
            var hashtags = "#Matric2026 #CAPS #NkanyeziLamp";
            var tags = $"{subject}, CAPS, Exam, Solutions, Teaching, Nkanyezi Lamp";
            var thumbnailText = $"{subject} | {examTitle}";
            return new YouTubeMetaResponse {
                Title = title,
                Description = description,
                Hashtags = hashtags,
                Tags = tags,
                ThumbnailText = thumbnailText
            };
        }
    }
}
