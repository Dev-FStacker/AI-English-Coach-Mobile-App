using BLL.Interface;
using Common.DTO;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class AzureTranslationService : ITranslationService
    {
        private readonly string _key = "D2WJtEfyvHncpcpokQeztdBGC0csb0n5WodUkROLMqAgICrCA0SHJQQJ99BAACYeBjFXJ3w3AAAbACOG3okl";
        private readonly string _endpoint = "https://api.cognitive.microsofttranslator.com/";
        private readonly string _location = "eastus";
        private readonly HttpClient _httpClient;

        public AzureTranslationService(IOptions<AzureTranslationSettings> settings)
        {
      
            _httpClient = new HttpClient();
        }

        public async Task<TranslationResult> TranslateTextAsync(string text, string targetLanguage, string sourceLanguage = "en")
        {
            try
            {
                string route = $"/translate?api-version=3.0&from={sourceLanguage}&to={targetLanguage}";

                var body = new object[] { new { Text = text } };
                var requestBody = JsonConvert.SerializeObject(body);

                using (var request = new HttpRequestMessage())
                {
                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(_endpoint + route);
                    request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    request.Headers.Add("Ocp-Apim-Subscription-Key", _key);
                    request.Headers.Add("Ocp-Apim-Subscription-Region", _location);

                    var response = await _httpClient.SendAsync(request);
                    var result = await response.Content.ReadAsStringAsync();

                    var translations = JsonConvert.DeserializeObject<List<TranslationResponse>>(result);

                    return new TranslationResult
                    {
                        OriginalText = text,
                        TranslatedText = translations[0].Translations[0].Text,
                        SourceLanguage = sourceLanguage,
                        TargetLanguage = targetLanguage,
                        Success = true
                    };
                }
            }
            catch (Exception ex)
            {
                return new TranslationResult
                {
                    OriginalText = text,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

    public class TranslationResponse
    {
        public List<Translation> Translations { get; set; }
    }

    public class Translation
    {
        public string Text { get; set; }
        public string To { get; set; }
    }
}
