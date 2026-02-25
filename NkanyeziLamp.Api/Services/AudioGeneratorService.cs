using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NkanyeziLamp.Api.Models;

namespace NkanyeziLamp.Api.Services
{
    public class AudioGeneratorService
    {
        private readonly SpeechOptions _options;

        public AudioGeneratorService(IOptions<SpeechOptions> options)
        {
            _options = options.Value ?? new SpeechOptions();
        }

        public async Task<List<WorkflowAudioFile>> GenerateAudioAsync(List<WorkflowNarrationScript> scripts)
        {
            var audioFiles = new List<WorkflowAudioFile>();
            var outputDir = Path.Combine("Audio");
            Directory.CreateDirectory(outputDir);
            string subscriptionKey = _options.AzureTtsKey;
            string region = _options.AzureRegion;
            if (string.IsNullOrWhiteSpace(subscriptionKey) || string.IsNullOrWhiteSpace(region))
            {
                return audioFiles;
            }
            foreach (var script in scripts)
            {
                var audioPath = Path.Combine(outputDir, $"{script.QuestionNumber}.mp3");
                // Use Azure TTS REST API
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                    var ttsEndpoint = $"https://{region}.tts.speech.microsoft.com/cognitiveservices/v1";
                    var ssml = $"<speak version='1.0' xml:lang='en-US'><voice xml:lang='en-US' xml:gender='Female' name='en-US-JennyNeural'>{System.Security.SecurityElement.Escape(script.VoiceText)}</voice></speak>";
                    var content = new System.Net.Http.StringContent(ssml, System.Text.Encoding.UTF8, "application/ssml+xml");
                    content.Headers.Add("X-Microsoft-OutputFormat", "audio-16khz-128kbitrate-mono-mp3");
                    var response = await client.PostAsync(ttsEndpoint, content);
                    response.EnsureSuccessStatusCode();
                    using (var fs = new FileStream(audioPath, FileMode.Create, FileAccess.Write))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }
                audioFiles.Add(new WorkflowAudioFile
                {
                    QuestionNumber = script.QuestionNumber,
                    AudioPath = audioPath
                });
            }
            return audioFiles;
        }
    }
}
