using BLL.Services;
using Common.DTO;
using DAL.Entities;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BLL.Interface
{
    public interface IAzureLanguageService
    {
        Task<GrammarCheckResult> CheckGrammarAsync(string text);
        Task<ConversationResponse> ProcessConversationAsync(string userMessage);
        Task<List<GrammarCheckResult>> AnalyzeConversationGrammarAsync();
        Task<GrammarCheckResult> CheckGrammarRealtime(string text);
        Task<UserEvaluation> EvaluateUserPerformanceAsync();
        Task<PronunciationResult> AssessPronunciationAsync(byte[] audioData);
        Task<ConversationResponse> StartTopicAsync(int topicId);
        Task<PronunciationAssessmentResult> EvaluatePronunciationAsync(Stream audioStream);
        Task<ConversationResponse> ProcessRealTimeConversationAsync( int topicId, byte[] audioData);
        Task<EvaluationResult> EndConversationAsync();
         Task<ConversationResponse> ProcessConversationWithTranslationAsync(
            string userMessage,
            string targetLanguage,
            string sourceLanguage = "en");
        void SetCurrentTopic(int topicId);
    }
}
