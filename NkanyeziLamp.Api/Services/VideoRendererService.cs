using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using System.Runtime.InteropServices;
using NkanyeziLamp.Api.Models;

namespace NkanyeziLamp.Api.Services
{
    public class VideoRendererService
    {
        public async Task<string> RenderVideoAsync(List<WorkflowSlide> slides, List<WorkflowAudioFile> audioFiles, LogoSettings logoSettings)
        {
            var videoDir = Path.Combine("Videos");
            Directory.CreateDirectory(videoDir);
            var videoPath = Path.Combine(videoDir, "TeachingVideo.mp4");

            // 1. Render slides to images
            var slideImages = new List<string>();
            for (int i = 0; i < slides.Count; i++)
            {
                var imgPath = Path.Combine(videoDir, $"slide_{i + 1}.png");
                // Use SkiaSharp or System.Drawing to render slide text/content to image (placeholder)
                using (var bmp = new System.Drawing.Bitmap(1920, 1080))
                using (var g = System.Drawing.Graphics.FromImage(bmp))
                {
                    g.Clear(System.Drawing.Color.White);
                    g.DrawString(slides[i].Title, new System.Drawing.Font("Segoe UI", 48), System.Drawing.Brushes.Black, 50, 50);
                    g.DrawString(slides[i].Content, new System.Drawing.Font("Segoe UI", 32), System.Drawing.Brushes.Black, 50, 200);
                    bmp.Save(imgPath, System.Drawing.Imaging.ImageFormat.Png);
                }
                slideImages.Add(imgPath);
            }

            // 2. Build FFmpeg command to combine images and audio
            // Concatenate audio files if multiple are provided
            string audioPath = null;
            if (audioFiles.Count > 0)
            {
                if (audioFiles.Count == 1)
                {
                    audioPath = audioFiles[0].AudioPath;
                }
                else
                {
                    var audioListPath = Path.Combine(videoDir, "audio.txt");
                    using (var sw = new StreamWriter(audioListPath))
                    {
                        foreach (var audio in audioFiles)
                        {
                            sw.WriteLine($"file '{audio.AudioPath.Replace("\\", "/")}'");
                        }
                    }
                    var concatAudioPath = Path.Combine(videoDir, "audio_concat.mp3");
                    var concatCmd = $"ffmpeg -y -f concat -safe 0 -i \"{audioListPath}\" -c copy \"{concatAudioPath}\"";
                    RunProcess(concatCmd);
                    audioPath = concatAudioPath;
                }
            }
            var imgListPath = Path.Combine(videoDir, "slides.txt");
            using (var sw = new StreamWriter(imgListPath))
            {
                foreach (var img in slideImages)
                {
                    sw.WriteLine($"file '{img.Replace("\\", "/")}'");
                    sw.WriteLine("duration 5"); // 5 seconds per slide
                }
                sw.WriteLine($"file '{slideImages[slideImages.Count - 1].Replace("\\", "/")}'");
            }

            // 3. Run FFmpeg to create video from images
            string ffmpegCmd;
            var logoEnabled = logoSettings != null
                && logoSettings.Enabled
                && !string.IsNullOrWhiteSpace(logoSettings.LogoPath)
                && File.Exists(logoSettings.LogoPath);

            var filter = "scale=1920:1080";
            if (logoEnabled)
            {
                var pos = BuildLogoPosition(logoSettings.Position);
                var scale = Math.Clamp(logoSettings.SizePercent, 5, 40) / 100.0;
                filter = $"[0:v]scale=1920:1080[base];[1:v]scale=iw*{scale}:ih*{scale}[logo];[base][logo]overlay={pos}";
            }

            if (!string.IsNullOrWhiteSpace(audioPath) && File.Exists(audioPath))
            {
                if (logoEnabled)
                {
                    ffmpegCmd = $"ffmpeg -y -f concat -safe 0 -i \"{imgListPath}\" -i \"{logoSettings.LogoPath}\" -i \"{audioPath}\" -filter_complex \"{filter}\" -c:v libx264 -r 30 -pix_fmt yuv420p -c:a aac -shortest \"{videoPath}\"";
                }
                else
                {
                    ffmpegCmd = $"ffmpeg -y -f concat -safe 0 -i \"{imgListPath}\" -i \"{audioPath}\" -c:v libx264 -r 30 -pix_fmt yuv420p -c:a aac -shortest -vf scale=1920:1080 \"{videoPath}\"";
                }
            }
            else
            {
                if (logoEnabled)
                {
                    ffmpegCmd = $"ffmpeg -y -f concat -safe 0 -i \"{imgListPath}\" -i \"{logoSettings.LogoPath}\" -filter_complex \"{filter}\" -c:v libx264 -r 30 -pix_fmt yuv420p \"{videoPath}\"";
                }
                else
                {
                    ffmpegCmd = $"ffmpeg -y -f concat -safe 0 -i \"{imgListPath}\" -c:v libx264 -r 30 -pix_fmt yuv420p -vf scale=1920:1080 \"{videoPath}\"";
                }
            }

            await RunProcessAsync(ffmpegCmd);

            return videoPath;
        }

        public async Task<string> RenderBoardVideoAsync(VideoProject project, List<WritingBlock> blocks, List<WorkflowAudioFile> audioFiles)
        {
            var videoDir = Path.Combine("Videos");
            Directory.CreateDirectory(videoDir);
            var videoPath = Path.Combine(videoDir, "TeachingBoard.mp4");

            var audioPath = PrepareAudio(audioFiles, videoDir);
            if (!string.IsNullOrWhiteSpace(audioPath) && File.Exists(audioPath))
            {
                var audioDuration = GetAudioDurationSeconds(audioPath);
                if (audioDuration > 1)
                {
                    ScaleBlocksToDuration(blocks, audioDuration);
                }
            }

            var totalDuration = Math.Max(10, blocks.Select(b => b.StartSeconds + b.DurationSeconds).DefaultIfEmpty(0).Max() + 2);

            var baseInput = $"-f lavfi -i \"color=c=#0b3d2e:s=1920x1080:d={totalDuration}\"";
            var logoEnabled = project?.Logo != null
                && project.Logo.Enabled
                && !string.IsNullOrWhiteSpace(project.Logo.LogoPath)
                && File.Exists(project.Logo.LogoPath);

            var drawChain = BuildDrawTextChain(blocks);
            if (string.IsNullOrWhiteSpace(drawChain))
            {
                drawChain = "scale=1920:1080";
            }

            string filter;
            if (logoEnabled)
            {
                var pos = BuildLogoPosition(project.Logo.Position);
                var scale = Math.Clamp(project.Logo.SizePercent, 5, 40) / 100.0;
                filter = $"[0:v]{drawChain}[board];[1:v]scale=iw*{scale}:ih*{scale}[logo];[board][logo]overlay={pos}[out]";
            }
            else
            {
                filter = drawChain;
            }

            string ffmpegCmd;
            if (!string.IsNullOrWhiteSpace(audioPath) && File.Exists(audioPath))
            {
                if (logoEnabled)
                {
                    ffmpegCmd = $"ffmpeg -y {baseInput} -i \"{project.Logo.LogoPath}\" -i \"{audioPath}\" -filter_complex \"{filter}\" -map \"[out]\" -map 2:a -c:v libx264 -r 30 -pix_fmt yuv420p -shortest \"{videoPath}\"";
                }
                else
                {
                    ffmpegCmd = $"ffmpeg -y {baseInput} -i \"{audioPath}\" -vf \"{filter}\" -c:v libx264 -r 30 -pix_fmt yuv420p -shortest \"{videoPath}\"";
                }
            }
            else
            {
                if (logoEnabled)
                {
                    ffmpegCmd = $"ffmpeg -y {baseInput} -i \"{project.Logo.LogoPath}\" -filter_complex \"{filter}\" -map \"[out]\" -c:v libx264 -r 30 -pix_fmt yuv420p \"{videoPath}\"";
                }
                else
                {
                    ffmpegCmd = $"ffmpeg -y {baseInput} -vf \"{filter}\" -c:v libx264 -r 30 -pix_fmt yuv420p \"{videoPath}\"";
                }
            }

            await RunProcessAsync(ffmpegCmd);
            return videoPath;
        }

        private static void RunProcess(string command)
        {
            var process = new Process
            {
                StartInfo = BuildShellStartInfo(command)
            };
            process.Start();
            process.WaitForExit();
        }

        private static async Task RunProcessAsync(string command)
        {
            var process = new Process
            {
                StartInfo = BuildShellStartInfo(command)
            };
            process.Start();
            await process.StandardError.ReadToEndAsync();
            process.WaitForExit();
        }

        private static string BuildLogoPosition(string position)
        {
            var margin = 24;
            return position switch
            {
                "top-left" => $"{margin}:{margin}",
                "top-right" => $"W-w-{margin}:{margin}",
                "bottom-left" => $"{margin}:H-h-{margin}",
                "bottom-right" => $"W-w-{margin}:H-h-{margin}",
                _ => $"W-w-{margin}:{margin}"
            };
        }

        private static string BuildDrawTextChain(List<WritingBlock> blocks)
        {
            var font = ResolveFontPath();
            var filters = new List<string>();
            foreach (var block in blocks)
            {
                var text = EscapeForDrawText(block.Text);
                var start = block.StartSeconds;
                var duration = block.DurationSeconds;
                var alpha = $"if(lt(t\\,{start})\\,0\\, if(lt(t\\,{start + duration})\\,(t-{start})/{Math.Max(0.2, duration)}\\,1))";
                var box = block.Highlight ? ":box=1:boxcolor=yellow@0.25:boxborderw=12" : "";
                filters.Add($"drawtext=fontfile='{font}':text='{text}':x={block.X}:y={block.Y}:fontsize={block.FontSize}:fontcolor={block.Color}:alpha='{alpha}'{box}");
            }
            return string.Join(",", filters);
        }

        private static string EscapeForDrawText(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            return input
                .Replace("\\", "\\\\")
                .Replace(":", "\\:")
                .Replace("'", "\\'")
                .Replace("%", "\\%")
                .Replace("\n", "\\n");
        }

        private static string PrepareAudio(List<WorkflowAudioFile> audioFiles, string videoDir)
        {
            if (audioFiles == null || audioFiles.Count == 0)
            {
                return null;
            }

            if (audioFiles.Count == 1)
            {
                return audioFiles[0].AudioPath;
            }

            var audioListPath = Path.Combine(videoDir, "audio.txt");
            using (var sw = new StreamWriter(audioListPath))
            {
                foreach (var audio in audioFiles)
                {
                    sw.WriteLine($"file '{audio.AudioPath.Replace("\\", "/")}'");
                }
            }
            var concatAudioPath = Path.Combine(videoDir, "audio_concat.mp3");
            var concatCmd = $"ffmpeg -y -f concat -safe 0 -i \"{audioListPath}\" -c copy \"{concatAudioPath}\"";
            RunProcess(concatCmd);
            return concatAudioPath;
        }

        private static double GetAudioDurationSeconds(string audioPath)
        {
            try
            {
                var probe = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffprobe",
                        Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{audioPath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                probe.Start();
                var output = probe.StandardOutput.ReadToEnd();
                probe.WaitForExit();
                if (double.TryParse(output.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var duration))
                {
                    return duration;
                }
            }
            catch
            {
                // If ffprobe is not available, fall back to text-based timing
            }
            return 0;
        }

        private static void ScaleBlocksToDuration(List<WritingBlock> blocks, double targetDuration)
        {
            if (blocks == null || blocks.Count == 0)
            {
                return;
            }

            var currentDuration = blocks.Max(b => b.StartSeconds + b.DurationSeconds);
            if (currentDuration <= 0)
            {
                return;
            }

            var scale = targetDuration / currentDuration;
            foreach (var block in blocks)
            {
                block.StartSeconds *= scale;
                block.DurationSeconds *= scale;
            }
        }

        private static ProcessStartInfo BuildShellStartInfo(string command)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }

            return new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }

        private static string ResolveFontPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "C\\\\:\\\\Windows\\\\Fonts\\\\arial.ttf";
            }

            return "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf";
        }
    }
}
