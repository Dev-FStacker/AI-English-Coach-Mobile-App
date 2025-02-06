using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities
{
    public class GrammarApiResponse
    {
        public string Corrections { get; set; }
        public List<string> Suggestions { get; set; }
    }
}
