using System;
using System.Collections.Generic;
using System.Linq;
using NkanyeziLamp.Api.Models;

namespace NkanyeziLamp.Api.Services
{
    public class BoardScriptService
    {
        private const double WordsPerSecond = 2.2;
        private const double DefaultPause = 0.6;

        public VideoProject BuildProject(string subject, List<WorkflowTeachingScript> scripts)
        {
            var project = new VideoProject
            {
                Subject = subject,
                IntroText = $"Welcome to Nkanyezi Lamp. Today we are solving CAPS {subject}.",
                OutroText = "If this helped you, please subscribe for more lessons."
            };

            project.Scenes.Add(BuildIntroScene(project));

            foreach (var script in scripts)
            {
                project.Scenes.Add(BuildQuestionScene(script));
                project.Scenes.Add(BuildTipScene(script));
            }

            project.Scenes.Add(BuildOutroScene(project));
            return project;
        }

        public void ApplyIntroOutroEdits(VideoProject project)
        {
            if (project == null || project.Scenes == null)
            {
                return;
            }

            var intro = project.Scenes.FirstOrDefault(s => s.Type == "intro");
            if (intro != null && intro.WritingBlocks.Count >= 2)
            {
                intro.WritingBlocks[1].Text = WrapText(project.IntroText ?? string.Empty, 48);
                intro.WritingBlocks[1].DurationSeconds = Math.Max(2.5, WordCount(intro.WritingBlocks[1].Text) / WordsPerSecond);
                RecalculateScene(intro);
            }

            var outro = project.Scenes.FirstOrDefault(s => s.Type == "outro");
            if (outro != null && outro.WritingBlocks.Count >= 2)
            {
                outro.WritingBlocks[1].Text = WrapText(project.OutroText ?? string.Empty, 48);
                outro.WritingBlocks[1].DurationSeconds = Math.Max(2.5, WordCount(outro.WritingBlocks[1].Text) / WordsPerSecond);
                RecalculateScene(outro);
            }
        }

        private static VideoScene BuildIntroScene(VideoProject project)
        {
            var blocks = new List<WritingBlock>();
            var cursor = new BoardCursor();

            blocks.Add(CreateBlock("Nkanyezi Lamp", cursor, 54, "white"));
            cursor.MoveNextLine();
            blocks.Add(CreateBlock(project.IntroText, cursor, 40, "white"));

            return BuildScene("intro", blocks);
        }

        private static VideoScene BuildQuestionScene(WorkflowTeachingScript script)
        {
            var blocks = new List<WritingBlock>();
            var cursor = new BoardCursor();

            blocks.Add(CreateBlock($"Question {script.QuestionNumber}", cursor, 48, "white"));
            cursor.MoveNextLine();
            blocks.Add(CreateBlock(script.RestatedQuestion ?? "Read the question carefully.", cursor, 40, "white", extraPause: 1.4));

            var steps = script.Steps?.Count > 0 ? script.Steps : ExtractSteps(script.DraftScript);
            foreach (var step in steps)
            {
                cursor.MoveNextLine();
                blocks.Add(CreateBlock(step, cursor, 38, "white"));
            }

            cursor.MoveNextLine();
            blocks.Add(CreateBlock("Final answer: see steps above.", cursor, 40, "yellow", highlight: true, extraPause: 1.0));

            return BuildScene("question", blocks, script.QuestionNumber);
        }

        private static VideoScene BuildTipScene(WorkflowTeachingScript script)
        {
            var blocks = new List<WritingBlock>();
            var cursor = new BoardCursor(startX: 120, startY: 140);
            var tipText = string.IsNullOrWhiteSpace(script.CommonMistakes)
                ? "Teaching tip: avoid skipping steps or rounding too early."
                : script.CommonMistakes;

            blocks.Add(CreateBlock("Teaching Tip", cursor, 42, "white"));
            cursor.MoveNextLine();
            blocks.Add(CreateBlock(tipText, cursor, 36, "white"));

            return BuildScene("tip", blocks, script.QuestionNumber);
        }

        private static VideoScene BuildOutroScene(VideoProject project)
        {
            var blocks = new List<WritingBlock>();
            var cursor = new BoardCursor();

            blocks.Add(CreateBlock("Nkanyezi Lamp", cursor, 54, "white"));
            cursor.MoveNextLine();
            blocks.Add(CreateBlock(project.OutroText, cursor, 40, "white"));

            return BuildScene("outro", blocks);
        }

        private static VideoScene BuildScene(string type, List<WritingBlock> blocks, string questionNumber = null)
        {
            var scene = new VideoScene
            {
                Type = type,
                QuestionNumber = questionNumber,
                WritingBlocks = blocks
            };
            RecalculateScene(scene);
            return scene;
        }

        private static WritingBlock CreateBlock(string text, BoardCursor cursor, int fontSize, string color, bool highlight = false, double extraPause = 0)
        {
            var safeText = WrapText(text ?? string.Empty, 48);
            var wordCount = WordCount(safeText);
            var duration = Math.Max(2.5, wordCount / WordsPerSecond) + extraPause;

            return new WritingBlock
            {
                Text = safeText,
                DurationSeconds = duration,
                X = cursor.X,
                Y = cursor.Y,
                FontSize = fontSize,
                Color = color,
                Highlight = highlight
            };
        }

        private static int WordCount(string text)
        {
            return text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private static void RecalculateScene(VideoScene scene)
        {
            var start = 0.0;
            foreach (var block in scene.WritingBlocks)
            {
                block.StartSeconds = start;
                start += block.DurationSeconds + DefaultPause;
            }
            scene.DurationSeconds = Math.Max(3, start);
        }

        private static string WrapText(string text, int maxCharsPerLine)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var lines = new List<string>();
            var line = string.Empty;

            foreach (var word in words)
            {
                if ((line + " " + word).Trim().Length > maxCharsPerLine)
                {
                    lines.Add(line.Trim());
                    line = word;
                }
                else
                {
                    line = string.IsNullOrWhiteSpace(line) ? word : $"{line} {word}";
                }
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                lines.Add(line.Trim());
            }

            return string.Join("\n", lines);
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
                if (trimmed.StartsWith("Step ", StringComparison.OrdinalIgnoreCase))
                {
                    steps.Add(trimmed);
                }
            }
            return steps;
        }

        private class BoardCursor
        {
            public int X { get; private set; }
            public int Y { get; private set; }
            private readonly int _lineHeight;

            public BoardCursor(int startX = 120, int startY = 140, int lineHeight = 64)
            {
                X = startX;
                Y = startY;
                _lineHeight = lineHeight;
            }

            public void MoveNextLine()
            {
                Y += _lineHeight;
            }
        }
    }
}
