using Common.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities
{
    public class ConversationResponse
    {
        public bool IsComplete { get; set; }
        public string BotResponse { get; set; }
        public string Evaluation { get; set; }
        public int CurrentTopic { get; set; }
        public int TurnsRemaining { get; set; }
        public string ScenarioPrompt { get; set; }
        public List<GrammarCheckResult> GrammarAnalysis { get; set; }
        public string AudioResponseUrl { get; set; }
        public byte[] AudioResponse {  get; set; }  
        public byte[] AudioData { get; set; }
        public string TranslatedResponse { get; set; }
        public string ResponseText { get; set; }

        public PronunciationResult PronunciationResult { get; set; }
    }
}
