using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NkanyeziLamp.Api.Models;

namespace NkanyeziLamp.Api.Services
{
    public class SlideGeneratorService
    {
        public List<WorkflowSlide> GenerateSlides(string subject, List<WorkflowTeachingScript> scripts, List<string> diagrams = null)
        {
            var slides = new List<WorkflowSlide>();
            int diagramIndex = 0;

            slides.Add(new WorkflowSlide
            {
                Title = "Nkanyezi Lamp",
                Content = $"CAPS Exam Solutions\nSubject: {subject}\nAutomated Teaching Video",
                DiagramPath = null
            });

            slides.Add(new WorkflowSlide
            {
                Title = "Instructions",
                Content = "Work through each question step-by-step.\nPause when needed and review the mark allocation.",
                DiagramPath = null
            });

            foreach (var script in scripts)
            {
                // Main question slide
                slides.Add(new WorkflowSlide {
                    Title = $"Question {script.QuestionNumber}",
                    Content = MarkMath(script.RestatedQuestion ?? script.DraftScript),
                    DiagramPath = diagrams != null && diagramIndex < diagrams.Count ? diagrams[diagramIndex++] : null
                });

                var steps = script.Steps?.Count > 0 ? script.Steps : ExtractSteps(script.DraftScript);
                if (steps.Count > 0)
                {
                    foreach (var step in steps.Select((value, index) => new { value, index }))
                    {
                        slides.Add(new WorkflowSlide
                        {
                            Title = $"Solution Step {step.index + 1} (Q{script.QuestionNumber})",
                            Content = MarkMath(step.value),
                            DiagramPath = diagrams != null && diagramIndex < diagrams.Count ? diagrams[diagramIndex++] : null
                        });
                    }
                }

                slides.Add(new WorkflowSlide {
                    Title = $"Final Answer (Q{script.QuestionNumber})",
                    Content = MarkMath(script.MarksBreakdown ?? script.TeachingExplanation ?? "Review the steps and confirm the final answer."),
                    DiagramPath = null
                });
            }

            slides.Add(new WorkflowSlide
            {
                Title = "Nkanyezi Lamp",
                Content = "If this helped you, please subscribe for more CAPS solutions.",
                DiagramPath = null
            });
            return slides;
        }

        // Mark math regions for frontend KaTeX/MathJax rendering
        private string MarkMath(string input)
        {
            // Example: wrap anything between $$...$$ as math
            // (In real use, improve detection or use AI to mark math)
            return (input ?? string.Empty).Replace("[math]", "$$").Replace("[/math]", "$$");
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
                if (Regex.IsMatch(trimmed, @"^Step\s*\d+[:\-]"))
                {
                    steps.Add(trimmed);
                }
            }
            return steps;
        }
    }
}
