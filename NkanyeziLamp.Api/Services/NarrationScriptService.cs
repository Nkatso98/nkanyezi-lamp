using System.Collections.Generic;
using System.Linq;
using System.Text;
using NkanyeziLamp.Api.Models;

namespace NkanyeziLamp.Api.Services
{
    public class NarrationScriptService
    {
        public List<WorkflowNarrationScript> GenerateNarrationScripts(string subject, List<WorkflowTeachingScript> scripts)
        {
            var narrations = new List<WorkflowNarrationScript>();
            foreach (var script in scripts)
            {
                var intro = $"Welcome to Nkanyezi Lamp. Today we are solving CAPS {subject}, Question {script.QuestionNumber}.";
                var body = BuildBody(script);
                var outro = "If this helped you, please subscribe for more lessons.";
                narrations.Add(new WorkflowNarrationScript
                {
                    QuestionNumber = script.QuestionNumber,
                    VoiceText = $"{intro}\n{body}\n{outro}"
                });
            }
            return narrations;
        }

        private static string BuildBody(WorkflowTeachingScript script)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Let's start by understanding what the question is asking.");
            if (!string.IsNullOrWhiteSpace(script.RestatedQuestion))
            {
                sb.AppendLine(script.RestatedQuestion);
            }
            var steps = script.Steps?.Count > 0 ? script.Steps : ExtractSteps(script.DraftScript);
            if (steps.Count > 0)
            {
                sb.AppendLine("Now, let's go step by step.");
                foreach (var step in steps.Select((value, index) => new { value, index }))
                {
                    sb.AppendLine($"Step {step.index + 1}: {step.value}");
                }
            }
            if (!string.IsNullOrWhiteSpace(script.CommonMistakes))
            {
                sb.AppendLine(script.CommonMistakes);
            }
            if (!string.IsNullOrWhiteSpace(script.MarksBreakdown))
            {
                sb.AppendLine(script.MarksBreakdown);
            }
            return sb.ToString().Trim();
        }

        private static List<string> ExtractSteps(string draftScript)
        {
            if (string.IsNullOrWhiteSpace(draftScript))
            {
                return new List<string>();
            }

            var lines = draftScript.Split('\n');
            var steps = new List<string>();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("Step ", System.StringComparison.OrdinalIgnoreCase))
                {
                    steps.Add(trimmed);
                }
            }
            return steps;
        }
    }
}
