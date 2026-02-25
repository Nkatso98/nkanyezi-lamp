using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NkanyeziLamp.Api.Models;

namespace NkanyeziLamp.Api.Services
{
    public class QuestionSplitterService
    {
        public List<ExtractedQuestion> SplitQuestions(string rawText)
        {
            var questions = new List<ExtractedQuestion>();
            var pattern = @"(?<=\n|^)(\d+(\.\d+)*)([\s\S]*?)(?=\n\d+(\.\d+)*|$)";
            var matches = Regex.Matches(rawText, pattern);
            foreach (Match match in matches)
            {
                var number = match.Groups[1].Value.Trim();
                var text = match.Groups[3].Value.Trim();
                var marks = ExtractMarks(text);
                questions.Add(new ExtractedQuestion
                {
                    QuestionNumber = number,
                    QuestionText = text,
                    Marks = marks
                });
            }
            return questions;
        }

        private static int? ExtractMarks(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var patterns = new[]
            {
                @"\((\d+)\s*marks?\)",
                @"\[(\d+)\s*marks?\]",
                @"\b(\d+)\s*marks?\b",
                @"\((\d+)\)"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Cast<Group>().Skip(1).Any(g => g.Success))
                {
                    foreach (Group group in match.Groups)
                    {
                        if (int.TryParse(group.Value, out var parsed))
                        {
                            return parsed;
                        }
                    }
                }
            }

            return null;
        }
    }
}
