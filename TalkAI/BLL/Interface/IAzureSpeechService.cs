// BLL/Interfaces/IAzureSpeechService.cs
namespace BLL.Interfaces
{
    public interface IAzureSpeechService
    {
     
        Task<byte[]> ConvertTextToSpeech(string text);
        Task<bool> SaveConversation(Guid userId, string audioContent, string textContent);
        Task<string> ConvertSpeechToTextFromBlobStorage(string blobUrl);

        Task<string> SaveAudioFile(byte[] audioData, string userId);
       
    }
}