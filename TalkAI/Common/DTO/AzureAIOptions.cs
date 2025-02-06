using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO
{
    public class AzureAIOptions
    {
        public string TextAnalyticsEndpoint { get; set; }
        public string TextAnalyticsApiKey { get; set; }
        public string OpenAIEndpoint { get; set; }
        public string OpenAIApiKey { get; set; }
    }
}
