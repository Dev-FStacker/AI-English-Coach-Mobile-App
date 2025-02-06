    // BLL/Services/AzureSpeechService.cs
    using Microsoft.CognitiveServices.Speech;
    using Microsoft.CognitiveServices.Speech.Audio;
    using Microsoft.Extensions.Configuration;
    using BLL.Interfaces;
    using DAL.Entities;
    using Microsoft.EntityFrameworkCore;
    using DAL.Data;


namespace BLL.Services
{
    public class AzureSpeechService : IAzureSpeechService
    {
        private readonly string _subscriptionKey;
        private readonly string _strorageKey;
        private readonly string _token;
        private readonly string _region;
        private readonly TalkAIContext _context;
        private readonly string _uploadPath;

        public AzureSpeechService(IConfiguration configuration, TalkAIContext context)
        {
            _token = configuration["AzureStorage:Token"] ?? string.Empty;
            _strorageKey = configuration["AzureStorage:Url"] ?? string.Empty;
            _subscriptionKey = configuration["Azure:SpeechService:SubscriptionKey"] ?? string.Empty;
            _region = configuration["Azure:SpeechService:Region"] ?? string.Empty;
            _context = context;
            _uploadPath = configuration["AudioFileSettings:UploadPath"] ?? string.Empty;
        }
        private string GenerateUniqueFileName(string userId)
        {
            return $"{userId}_{DateTime.UtcNow.Ticks}.nav";
        }


        public async Task<byte[]> ConvertTextToSpeech(string text)
        {
            var config = SpeechConfig.FromSubscription(_subscriptionKey, _region);
            config.SpeechSynthesisVoiceName = "en-US-JennyNeural";

            using var synthesizer = new SpeechSynthesizer(config);
            var result = await synthesizer.SpeakTextAsync(text);
            return result.AudioData;
        }

        public async Task<bool> SaveConversation(Guid userId, string audioContent, string textContent)
        {
            try
            {
                var conversation = new Conversation
                {
                    UserId = userId,
                    AudioFilePath = audioContent,
                    TextContent = textContent,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
       public async Task<string> SaveAudioFile(byte[] audioData, string userId)
        {
            var fileName = GenerateUniqueFileName(userId);
            var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), _uploadPath);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var filePath = Path.Combine(directoryPath, fileName);
            await File.WriteAllBytesAsync(filePath, audioData);
            return fileName;
        }
        public async Task<string> ConvertSpeechToTextFromBlobStorage(string blobUrl)
        {
            try
            {
                var config = SpeechConfig.FromSubscription(_subscriptionKey, _region);
                config.SpeechRecognitionLanguage = "en-US";

                // Download the audio file first
                using var client = new HttpClient();
                using var stream = await client.GetStreamAsync(blobUrl);
                using var audioInputStream = AudioInputStream.CreatePushStream();
                using var audioConfig = AudioConfig.FromStreamInput(audioInputStream);
                using var recognizer = new SpeechRecognizer(config, audioConfig);

                // Copy the audio data to the push stream
                byte[] buffer = new byte[8192];
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    audioInputStream.Write(buffer, bytesRead);
                }
                audioInputStream.Close();

                var result = await recognizer.RecognizeOnceAsync();

                if (result.Reason == ResultReason.RecognizedSpeech)
                {
                    return result.Text;
                }

                throw new Exception($"Speech recognition failed: {result.Reason}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error converting speech to text: {ex.Message}");
            }
        }

    }
    } 