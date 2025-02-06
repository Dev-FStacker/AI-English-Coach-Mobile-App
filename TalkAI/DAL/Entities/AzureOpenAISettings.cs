using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities
{
    public class AzureOpenAISettings
    {
        public string Endpoint { get; set; }
        public string ApiKey { get; set; }
        public string DeploymentName { get; set; }
    }
}
