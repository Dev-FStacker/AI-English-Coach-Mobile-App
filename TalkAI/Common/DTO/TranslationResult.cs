using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO
{
    public class TranslationResult
    {
        public string OriginalText { get; set; }
        public string TranslatedText { get; set; }
        public string SourceLanguage { get; set; }
        public string TargetLanguage { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}
