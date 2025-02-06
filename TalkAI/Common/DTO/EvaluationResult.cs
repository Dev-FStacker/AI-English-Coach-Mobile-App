using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO
{
    public class EvaluationResult
    {
        public int Grammar { get; set; }
        public int Vocabulary { get; set; }
        public int Fluency { get; set; }
        public int Overall { get; set; }
        public string Suggestions { get; set; }
    }
}
