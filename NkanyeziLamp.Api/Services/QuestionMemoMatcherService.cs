using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NkanyeziLamp.Api.Models;

namespace NkanyeziLamp.Api.Services
{
    public class QuestionMemoMatcherService
    {
        public List<MatchedQuestion> MatchQuestions(List<ExtractedQuestion> questions, List<ExtractedQuestion> memos)
        {
            var matched = new List<MatchedQuestion>();
            foreach (var q in questions)
            {
                var exact = memos.FirstOrDefault(m => m.QuestionNumber == q.QuestionNumber);
                if (exact != null)
                {
                    matched.Add(BuildMatch(q, exact, "number_match", 1.0, false));
                    continue;
                }

                var structural = memos.FirstOrDefault(m =>
                    m.QuestionNumber.StartsWith(q.QuestionNumber + ".", StringComparison.OrdinalIgnoreCase) ||
                    q.QuestionNumber.StartsWith(m.QuestionNumber + ".", StringComparison.OrdinalIgnoreCase));

                if (structural != null)
                {
                    var score = Similarity(q.QuestionText, structural.QuestionText);
                    matched.Add(BuildMatch(q, structural, "structure_match", score, score < 0.2));
                    continue;
                }

                var best = memos
                    .Select(m => new { memo = m, score = Similarity(q.QuestionText, m.QuestionText) })
                    .OrderByDescending(m => m.score)
                    .FirstOrDefault();

                if (best == null || best.score < 0.2)
                {
                    matched.Add(new MatchedQuestion
                    {
                        QuestionNumber = q.QuestionNumber,
                        QuestionText = q.QuestionText,
                        AnswerText = null,
                        Marks = q.Marks,
                        NeedsReview = true,
                        SimilarityScore = best?.score ?? 0,
                        MatchReason = "unmatched"
                    });
                    continue;
                }

                matched.Add(BuildMatch(q, best.memo, "similarity_match", best.score, best.score < 0.35));
            }
            return matched;
        }

        private static MatchedQuestion BuildMatch(ExtractedQuestion question, ExtractedQuestion memo, string reason, double score, bool needsReview)
        {
            return new MatchedQuestion
            {
                QuestionNumber = question.QuestionNumber,
                QuestionText = question.QuestionText,
                AnswerText = memo?.QuestionText,
                Marks = question.Marks,
                NeedsReview = needsReview,
                SimilarityScore = score,
                MatchReason = reason
            };
        }

        private static double Similarity(string a, string b)
        {
            var tokensA = Tokenize(a);
            var tokensB = Tokenize(b);

            if (tokensA.Count == 0 || tokensB.Count == 0)
            {
                return 0;
            }

            var intersect = tokensA.Intersect(tokensB).Count();
            var union = tokensA.Union(tokensB).Count();
            return union == 0 ? 0 : (double)intersect / union;
        }

        private static HashSet<string> Tokenize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return new HashSet<string>();
            }

            var cleaned = Regex.Replace(input.ToLowerInvariant(), @"[^a-z0-9\s]", " ");
            return cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        }
    }
}
