using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO
{
    public class PronunciationResult
    {
        public double AccuracyScore { get; set; }
        public double FluencyScore { get; set; }
        public double ProsodyScore { get; set; }
        public double PronunciationScore { get; set; }
        public string ErrorDetails { get; set; }
    }
}
