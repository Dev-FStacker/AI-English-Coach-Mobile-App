using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO
{
 
    public class GrammarCheckResult
    {
        public string OriginalText { get; set; }
        public string CorrectedText { get; set; }
        public string Suggestions { get; set; }
        public List<string> KeyPhrases { get; set; }
        public string Sentiment { get; set; }
    }
}
