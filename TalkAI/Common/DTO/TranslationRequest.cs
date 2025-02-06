using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO
{
    public class TranslationRequest
    {
        public string Message { get; set; }
        public string TargetLanguage { get; set; }
        public string SourceLanguage { get; set; } = "en";
    }
}
