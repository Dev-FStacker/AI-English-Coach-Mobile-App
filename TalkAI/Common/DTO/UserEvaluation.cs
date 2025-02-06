using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO
{
    public class UserEvaluation
    {
        public int RelevanceScore { get; set; }
        public int GrammarScore { get; set; }
        public int VocabularyScore { get; set; }
        public int CommunicationScore { get; set; }
        public int OverallScore { get; set; }
        public string Suggestions { get; set; }
    }
}
