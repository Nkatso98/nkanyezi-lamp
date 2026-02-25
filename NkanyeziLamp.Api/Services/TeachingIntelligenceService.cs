using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NkanyeziLamp.Api.Models;

namespace NkanyeziLamp.Api.Services
{
    public class TeachingIntelligenceService
    {
        public async Task<List<WorkflowTeachingScript>> GenerateTeachingScriptsAsync(List<MatchedQuestion> matchedQuestions)
        {
            var scripts = new List<WorkflowTeachingScript>();
            foreach (var mq in matchedQuestions)
            {
                if (mq.NeedsReview || string.IsNullOrWhiteSpace(mq.AnswerText))
                {
                    scripts.Add(new WorkflowTeachingScript
                    {
                        QuestionNumber = mq.QuestionNumber,
                        RestatedQuestion = mq.QuestionText,
                        TeachingExplanation = "No memo answer found. Please review and edit manually.",
                        CommonMistakes = "Check the memo alignment and confirm the correct answer text.",
                        MarksBreakdown = mq.Marks.HasValue ? $"Total marks: {mq.Marks}" : "Mark allocation unavailable.",
                        DraftScript = BuildDraftScript(mq, new List<string>(), "No memo answer found. Please review manually.", "Check memo alignment and confirm correct answer text.", mq.Marks)
                    });
                    continue;
                }

                var steps = BuildSteps(mq);
                var teachingExplanation = BuildTeachingExplanation(mq, steps);
                var commonMistakes = "Common mistakes: mixing up units, skipping substitution steps, or rounding too early.";
                var marksBreakdown = mq.Marks.HasValue
                    ? $"Marks breakdown: {mq.Marks} total. Allocate marks for correct formula, substitution, and final answer."
                    : "Marks breakdown: Allocate marks for correct formula, substitution, and final answer.";

                scripts.Add(new WorkflowTeachingScript
                {
                    QuestionNumber = mq.QuestionNumber,
                    RestatedQuestion = mq.QuestionText,
                    Steps = steps,
                    TeachingExplanation = teachingExplanation,
                    CommonMistakes = commonMistakes,
                    MarksBreakdown = marksBreakdown,
                    DraftScript = BuildDraftScript(mq, steps, teachingExplanation, commonMistakes, mq.Marks)
                });
            }
            return scripts;
        }

        private static List<string> BuildSteps(MatchedQuestion mq)
        {
            var steps = new List<string>
            {
                "Identify what the question is asking and list the known quantities.",
                "Select the correct CAPS-approved formula or principle.",
                "Substitute the given values carefully and show each step.",
                "Simplify the expression and check units where applicable."
            };

            if (!string.IsNullOrWhiteSpace(mq.AnswerText))
            {
                steps.Add($"Use the memo guidance: {mq.AnswerText.Trim()}");
            }

            return steps;
        }

        private static string BuildTeachingExplanation(MatchedQuestion mq, List<string> steps)
        {
            var sb = new StringBuilder();
            sb.AppendLine("CAPS-aligned reasoning:");
            sb.AppendLine("We follow the official CAPS method, showing each step clearly and justifying the formula used.");
            sb.AppendLine();
            sb.AppendLine("Teaching explanation:");
            sb.AppendLine($"Restated question: {mq.QuestionText}");
            sb.AppendLine("Step-by-step solution:");
            foreach (var step in steps.Select((value, index) => new { value, index }))
            {
                sb.AppendLine($"Step {step.index + 1}: {step.value}");
            }
            return sb.ToString().Trim();
        }

        private static string BuildDraftScript(MatchedQuestion mq, List<string> steps, string teachingExplanation, string commonMistakes, int? marks)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Restated question: {mq.QuestionText}");
            sb.AppendLine();
            if (steps.Count > 0)
            {
                sb.AppendLine("Steps:");
                foreach (var step in steps.Select((value, index) => new { value, index }))
                {
                    sb.AppendLine($"Step {step.index + 1}: {step.value}");
                }
                sb.AppendLine();
            }
            sb.AppendLine(teachingExplanation);
            sb.AppendLine();
            sb.AppendLine(commonMistakes);
            sb.AppendLine(marks.HasValue ? $"Marks breakdown: {marks} total. Allocate marks for correct formula, substitution, and final answer." : "Marks breakdown: Allocate marks for correct formula, substitution, and final answer.");
            return sb.ToString().Trim();
        }
    }
}
