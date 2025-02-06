using Azure.AI.TextAnalytics;
using Azure.AI.OpenAI;
using BLL.Interface;
using Common.DTO;
using DAL.Entities;
using Microsoft.Extensions.Options;
using System.Text;
using System.Threading.Tasks;
using OpenAI.Chat;
using Azure.AI.OpenAI.Chat;
using Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text.RegularExpressions;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using Microsoft.Extensions.Configuration;
using Microsoft.CognitiveServices.Speech.Audio;

namespace BLL.Services
{
    public class AzureLanguageService : IAzureLanguageService
    {
        private readonly IAzureStorageService _storageService;
        private readonly SpeechConfig _speechConfig;
        
        private readonly PronunciationAssessmentConfig _pronunciationConfig;
        private readonly ICacheService _cacheService;
        private readonly TextAnalyticsClient _textAnalyticsClient;
        private readonly AzureOpenAIClient _openAIClient;
        private readonly string _deploymentName = "gpt-4";
        private readonly Dictionary<int, TopicTemplate> _topicTemplates;
        private int _conversationTurnCount = 0;
        private const int MaxTurns = 3;
        private int _currentTopicId;
        private List<(string Message, bool IsBot)> _conversationHistory = new List<(string, bool)>();
        private string _characterRole = "default role";
        private List<string> _userMessages = new List<string>();
        private List<string> _botMessages = new List<string>();
        private readonly ITranslationService _translationService;
        // Semaphore để giới hạn số lượng yêu cầu đồng thời
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(10);
        private readonly string speechKey = "6wjb3AL1IhtqisKAtSlzQOulNXYSbJMNqWrxVDARRKlttP7QTM5pJQQJ99BAACYeBjFXJ3w3AAABACOGwurr";
        private readonly string speechRegion = "eastus";
        public AzureLanguageService(
            ICacheService cacheService,
            ITranslationService translationService,
            IAzureStorageService storageService
         )
        {
            _storageService = storageService;
            _cacheService = cacheService;
            _translationService = translationService;

            // Khởi tạo SpeechConfig với key và region được truyền trực tiếp
            _speechConfig = SpeechConfig.FromSubscription("6wjb3AL1IhtqisKAtSlzQOulNXYSbJMNqWrxVDARRKlttP7QTM5pJQQJ99BAACYeBjFXJ3w3AAABACOGwurr", "eastus");
            _speechConfig.SpeechSynthesisVoiceName = "en-US-JennyNeural";
            _speechConfig.SetProperty(PropertyId.Speech_LogFilename, "SpeechSDK.log");
            // Khởi tạo topic templates
            _topicTemplates = new Dictionary<int, TopicTemplate>
            {
                [1] = new TopicTemplate { InitialPrompt = "Daily routines", FollowUpQuestions = new List<string>(), CharacterRole = "friendly roommate" },
                [2] = new TopicTemplate { InitialPrompt = "Travel experiences", FollowUpQuestions = new List<string>(), CharacterRole = "experienced traveler" },
                [3] = new TopicTemplate { InitialPrompt = "Tech innovations", FollowUpQuestions = new List<string>(), CharacterRole = "tech enthusiast" },
                [4] = new TopicTemplate { InitialPrompt = "Career development", FollowUpQuestions = new List<string>(), CharacterRole = "career coach" }
            };

            // Khởi tạo TextAnalyticsClient
            string textAnalyticsEndpoint = "https://talkaii.cognitiveservices.azure.com/";
            string textAnalyticsApiKey = "9fkB8CZ6uHTbA6K7EotcyO9Xx5gioJCfiUyzOlQ2GPxEprCketLVJQQJ99BAACYeBjFXJ3w3AAAaACOGn0Rb";
            _textAnalyticsClient = new TextAnalyticsClient(new Uri(textAnalyticsEndpoint), new AzureKeyCredential(textAnalyticsApiKey));

            // Khởi tạo AzureOpenAIClient
            string openAIEndpoint = "https://benns-m5xr241b-swedencentral.cognitiveservices.azure.com/";
            string openAIApiKey = "FIzCm76QUqt9Djb7nmlpimVxVoowphtvfjYuZQ7yAqBcfPaH5wZTJQQJ99BAACfhMk5XJ3w3AAAAACOGWDM6";
            _openAIClient = new AzureOpenAIClient(new Uri(openAIEndpoint), new AzureKeyCredential(openAIApiKey));

            // Khởi tạo PronunciationAssessmentConfig
            _pronunciationConfig = new PronunciationAssessmentConfig(
                referenceText: "",
                gradingSystem: GradingSystem.HundredMark,
                granularity: Granularity.Phoneme,
                enableMiscue: false);
            _pronunciationConfig.EnableProsodyAssessment();
        }
    
    private bool ValidateTopic(int topicId)
        {
            if (!_topicTemplates.ContainsKey(topicId))
            {
           
                return false;
            }
            return true;
        }
        public async Task<ConversationResponse> ProcessRealTimeConversationAsync( int topicId, byte[] audioData)
        {
            await _semaphore.WaitAsync();
            try
            {
                SetCurrentTopic(topicId);
                // 1. Upload audio to Azure Storage
                var fileName = await _storageService.UploadAudioFile(audioData);
                var latestFileName = await _storageService.GetLatestAudioFileName(fileName);

                // 2. Convert from cloud storage
                var text = await ConvertSpeechFromCloudFile(fileName);
                // 3. Process with AI
                var response = await GenerateBotResponse(text, topicId);

                // 4. Convert response to speech
                response.AudioResponse = await ConvertTextToSpeech(response.AudioResponseUrl);

                return response;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<string> ConvertSpeechFromCloudFile(string fileName)
        {
            try
            {

                
                // SỬA: Gọi GetAudioBytes() để lấy dữ liệu audio dạng byte[]
                var audioBytes = await _storageService.GetAudioBytes(fileName); 
                //Console.WriteLine($"Kích thước audioBytes: {audioBytes.Length} bytes");
                Console.WriteLine("SpeakSomething");
                //// Sử dụng audioBytes (byte[]) để tạo AudioConfig
                var audioConfig = AudioConfig.FromWavFileInput(audioBytes.ToString());
             
                using var recognizer = new SpeechRecognizer(_speechConfig, audioConfig);
                var result = await recognizer.RecognizeOnceAsync();

                   if (result.Reason == ResultReason.RecognizedSpeech)
                {
                    Console.WriteLine(result.Text);
                }
                return result.Text;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi ConvertSpeechFromCloudFile: {ex}");
                return string.Empty;
            }
        }


        private async Task<PronunciationResult> AnalyzePronunciation(string text)
        {
            // Triển khai phân tích phát âm
            return new PronunciationResult
            {
                AccuracyScore = 95.0,
                FluencyScore = 90.5,
                ProsodyScore = 88.0
            };
        }

        private async Task<string> ConvertSpeechToText(byte[] audioData)
        {
            try
            {
                var audioConfig = AudioConfig.FromStreamInput(
                    AudioInputStream.CreatePushStream(
                        AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1)
                    )
                );

                using var recognizer = new SpeechRecognizer(_speechConfig, audioConfig);
                var result = await recognizer.RecognizeOnceAsync();

                return result.Text;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Speech recognition error: {ex}");
                return string.Empty;
            }
        }

        private async Task<byte[]> ConvertTextToSpeech(string text)
        {
            try
            {
                using var synthesizer = new SpeechSynthesizer(_speechConfig, null);
                using var result = await synthesizer.SpeakTextAsync(text);
                return result.AudioData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Text-to-speech error: {ex}");
                return Array.Empty<byte>();
            }
        }
        public async Task<PronunciationAssessmentResult> EvaluatePronunciationAsync(Stream audioStream)
        {
            // Tạo audio input stream từ Stream
            var audioFormat = AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1); // Điều chỉnh theo định dạng audio
            var audioInputStream = AudioInputStream.CreatePullStream(new BinaryAudioStreamReader(audioStream), audioFormat);

            using var audioConfig = AudioConfig.FromStreamInput(audioInputStream);
            using var recognizer = new SpeechRecognizer(_speechConfig, "en-US", audioConfig);

            _pronunciationConfig.ApplyTo(recognizer);

            var result = await recognizer.RecognizeOnceAsync();
            return PronunciationAssessmentResult.FromResult(result);
        }

        // Triển khai custom stream reader
        public class BinaryAudioStreamReader : PullAudioInputStreamCallback
        {
            private readonly Stream _audioStream;

            public BinaryAudioStreamReader(Stream audioStream)
            {
                _audioStream = audioStream;
            }

            public override int Read(byte[] buffer, uint size)
            {
                return _audioStream.Read(buffer, 0, (int)size);
            }

            protected override void Dispose(bool disposing)
            {
                _audioStream.Dispose();
                base.Dispose(disposing);
            }
        }

        // Triển khai interface mới
        public async Task<PronunciationResult> AssessPronunciationAsync(byte[] audioData)
        {
            using var stream = new MemoryStream(audioData);
            var assessment = await EvaluatePronunciationAsync(stream);

            return new PronunciationResult
            {
                AccuracyScore = assessment.AccuracyScore,
                FluencyScore = assessment.FluencyScore,
                ProsodyScore = assessment.ProsodyScore,
                PronunciationScore = assessment.PronunciationScore,
                
            };
        }
    
    public void SetCurrentTopic(int topicId)
        {
            if (!_topicTemplates.ContainsKey(topicId))
                throw new ArgumentException("Invalid topic ID", nameof(topicId));

            _currentTopicId = topicId;
        }

        public async Task<ConversationResponse> ProcessConversationAsync(string userMessage)
        {
            await _semaphore.WaitAsync(); // Giới hạn số lượng yêu cầu đồng thời
            try
            {
                if (!ValidateTopic(_currentTopicId))
                {
                    return new ConversationResponse
                    {
                        IsComplete = true,
                        BotResponse = "Invalid conversation topic",
                        TurnsRemaining = 0
                    };
                }

                if (IsGoodbyeMessage(userMessage))
                    return await HandleGoodbyeMessage();

                _conversationTurnCount++;
                int turnsRemaining = MaxTurns - _conversationTurnCount;

                // Kiểm tra cache trước khi gọi API
                var cachedResponse = await _cacheService.GetCachedResponse(userMessage, _currentTopicId);
                if (cachedResponse != null)
                    return cachedResponse;

                _userMessages.Add(userMessage);
                _conversationHistory.Add((userMessage, false));

                if (turnsRemaining <= 0)
                    return await HandleConversationEnd();

                if (!_topicTemplates.ContainsKey(_currentTopicId))
                    return new ConversationResponse { IsComplete = true, BotResponse = "Invalid topic. Please start a new conversation.", TurnsRemaining = 0 };

                var (isRelevant, suggestedRedirect) = await CheckTopicRelevance(userMessage);
                if (!isRelevant)
                    return await HandleIrrelevantMessage(userMessage, suggestedRedirect, turnsRemaining);

                return await GenerateBotResponse(userMessage, turnsRemaining);
            }
            finally
            {
                _semaphore.Release(); // Giải phóng semaphore
            }
        }

        public async Task<ConversationResponse> ProcessConversationWithTranslationAsync(
            string userMessage,
            string targetLanguage,
            string sourceLanguage = "en")
        {
            // First translate user message to English if needed
            if (sourceLanguage != "en")
            {
                var translatedInput = await _translationService.TranslateTextAsync(
                    userMessage,
                    "en",
                    sourceLanguage
                );
                userMessage = translatedInput.TranslatedText;
            }

            // Process conversation normally
            var response = await ProcessConversationAsync(userMessage);

            // Translate response if needed
            if (targetLanguage != "en")
            {
                var translatedOutput = await _translationService.TranslateTextAsync(
                    response.BotResponse,
                    targetLanguage,
                    "en"
                );
                response.BotResponse = translatedOutput.TranslatedText;
            }

            return response;
        }
    

    private bool IsGoodbyeMessage(string message)
        {
            return message.Equals("Bye", StringComparison.OrdinalIgnoreCase) ||
                   message.Equals("Goodbye", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<ConversationResponse> HandleGoodbyeMessage()
        {
            var evaluation = await EvaluateConversationAsync();
            var grammarAnalysis = await AnalyzeConversationGrammarAsync();

            return new ConversationResponse
            {
                IsComplete = true,
                BotResponse = "Goodbye! It was great talking with you.",
                Evaluation = evaluation,
                GrammarAnalysis = grammarAnalysis,
                TurnsRemaining = 0
            };
        }

        private async Task<ConversationResponse> HandleConversationEnd()
        {
            var evaluationTask = EvaluateConversationAsync();
            var finalAnalysisTask = AnalyzeConversationGrammarAsync();

            await Task.WhenAll(evaluationTask, finalAnalysisTask);

            var farewellMessage = $"Thank you for the conversation! It was great discussing {_topicTemplates[_currentTopicId].InitialPrompt}. Have a wonderful day!";
            return new ConversationResponse
            {
                IsComplete = true,
                BotResponse = farewellMessage,
                Evaluation = evaluationTask.Result,
                CurrentTopic = _currentTopicId,
                TurnsRemaining = 0
            };
        }

        private async Task<ConversationResponse> HandleIrrelevantMessage(string userMessage, string suggestedRedirect, int turnsRemaining)
        {
            var redirectPromptTemplate = $@"Create a natural response that: 
1. Acknowledges: '{userMessage}' 
2. Redirects conversation: {suggestedRedirect} 
3. Asks a relevant question about {_topicTemplates[_currentTopicId].InitialPrompt} 
4. Keeps response under 40 words 
5. Stays in character as {_characterRole} and maintains the context of the conversation.";

            var redirectMessages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(redirectPromptTemplate)
            };

            var redirectChatOptions = new ChatCompletionOptions
            {
                Temperature = 0.2f,
                MaxOutputTokenCount = 100
            };

            var redirectResponse = await _openAIClient.GetChatClient(_deploymentName)
                .CompleteChatAsync(redirectMessages, redirectChatOptions);

            var redirectBotResponse = redirectResponse?.Value?.Content?.FirstOrDefault()?.Text
                ?? $"That's interesting! Let's focus back on {_topicTemplates[_currentTopicId].InitialPrompt}. {_topicTemplates[_currentTopicId].FollowUpQuestions[0]}";

            _botMessages.Add(redirectBotResponse);
            _conversationHistory.Add((redirectBotResponse, true));
            _conversationTurnCount++;

            return new ConversationResponse
            {
                IsComplete = false,
                BotResponse = redirectBotResponse,
                CurrentTopic = _currentTopicId,
                TurnsRemaining = turnsRemaining
            };
        }

        private async Task<ConversationResponse> GenerateBotResponse(string userMessage, int turnsRemaining)
        {
            var conversationContext = string.Join("\n",
                _conversationHistory
                    .TakeLast(4)
                    .Select(m => $"{(m.IsBot ? "Bot" : "User")}: {m.Message}")
            );

            var systemPrompt = $@"
Role: {_characterRole}
Rules:
- You are a real {_characterRole}, not an AI
- Stay in character and on topic: {_topicTemplates[_currentTopicId].InitialPrompt}
- Use natural, conversational language
- Keep responses brief (1-2 sentences)
- Correct grammar subtly

Context:
{string.Join(" ", _userMessages)}

Examples:
- User: How are you?
- Bot: I'm great, thanks! How about you?
- User: What's your favorite color?
- Bot: I love blue! What's yours?";

            var chatMessages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(systemPrompt),
                ChatMessage.CreateUserMessage(userMessage)
            };

            var processChatOptions = new ChatCompletionOptions
            {
                Temperature = 0.2f,
                MaxOutputTokenCount = 50,
                FrequencyPenalty = 0.2f,
                PresencePenalty = 0.2f
            };

            var response = await _openAIClient.GetChatClient(_deploymentName)
                .CompleteChatAsync(chatMessages, processChatOptions);

            var processBotResponse = response?.Value?.Content?.FirstOrDefault()?.Text
                ?? "I apologize, could you rephrase that?";

            _botMessages.Add(processBotResponse);
            _conversationHistory.Add((processBotResponse, true));
            _conversationTurnCount++;

            var result = new ConversationResponse
            {
                IsComplete = false,
                BotResponse = processBotResponse,
                CurrentTopic = _currentTopicId,
                TurnsRemaining = turnsRemaining
            };

            await _cacheService.CacheResponse(userMessage, _currentTopicId, result);

            return result;
        }

        private async Task<(bool isRelevant, string suggestedRedirect)> CheckTopicRelevance(string message)
        {
            var promptTemplate = $@"You are a {_characterRole} discussing {_topicTemplates[_currentTopicId].InitialPrompt}.
Analyze the user's message with these criteria:
1. Is the message directly related to the current topic?
2. If not related, how can you subtly redirect the conversation?
3. Maintain your character's perspective

Return in strict JSON format:
{{
    ""isRelevant"": boolean,
    ""suggestedRedirect"": string (a creative way to bring conversation back to topic)
}}";

            var chatMessages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(promptTemplate),
                ChatMessage.CreateUserMessage(message)
            };

            var chatOptions = new ChatCompletionOptions
            {
                Temperature = 0.2f,
                MaxOutputTokenCount = 50,
                FrequencyPenalty = 0.2f,
                PresencePenalty = 0.2f
            };

            var response = await _openAIClient.GetChatClient(_deploymentName)
                .CompleteChatAsync(chatMessages, chatOptions);

            try
            {
                var responseText = response?.Value?.Content?.FirstOrDefault()?.Text ?? "{}";
                var jsonResponse = System.Text.Json.JsonDocument.Parse(responseText);
                var isRelevant = jsonResponse.RootElement.GetProperty("isRelevant").GetBoolean();
                var suggestedRedirect = jsonResponse.RootElement.GetProperty("suggestedRedirect").GetString() ?? "";

                return (isRelevant, suggestedRedirect);
            }
            catch
            {
                return (true, string.Empty);
            }
        }

  

public async Task<EvaluationResult> EndConversationAsync()
{
    var evaluation = await EvaluateUserPerformanceAsync();

            return new EvaluationResult
            {
                Grammar = evaluation.GrammarScore,
                Vocabulary = evaluation.VocabularyScore,
              Fluency = evaluation.CommunicationScore,
                Overall = evaluation.OverallScore,
                Suggestions = evaluation.Suggestions,
    };
        }
        public async Task<UserEvaluation> EvaluateUserPerformanceAsync()
        {
            var systemPrompt = $@"
You are a strict English examiner. Provide scores (0-10) and feedback in THIS EXACT FORMAT:

Relevance: [score]
Grammar: [score]
Vocabulary: [score]
Fluency: [score]
Overall: [score]
Suggestions: [bullet points]

Analyze this conversation:
{string.Join("\n", _conversationHistory.Select(m => $"{(m.IsBot ? "Bot" : "User")}: {m.Message}"))}";

            var chatMessages = new List<ChatMessage>
    {
        ChatMessage.CreateSystemMessage(systemPrompt),
        ChatMessage.CreateUserMessage("Provide a detailed evaluation and suggestions for improvement.")
    };

            var chatOptions = new ChatCompletionOptions
            {
                Temperature = 0.2f,
                MaxOutputTokenCount = 200
            };

            var evaluationResponse = await _openAIClient.GetChatClient(_deploymentName)
                .CompleteChatAsync(chatMessages, chatOptions);

            var evaluationText = evaluationResponse?.Value?.Content?.FirstOrDefault()?.Text ?? "Evaluation not available.";

            return ParseEvaluationResponse(evaluationText);
        }

        private UserEvaluation ParseEvaluationResponse(string evaluationText)
        {
            var evaluation = new UserEvaluation();

            // Trích xuất scores với regex linh hoạt
            var grammarMatch = Regex.Match(evaluationText, @"Grammar:\s*(\d+)");
            var vocabMatch = Regex.Match(evaluationText, @"Vocabulary:\s*(\d+)");
            var fluencyMatch = Regex.Match(evaluationText, @"Fluency:\s*(\d+)");
            var overallMatch = Regex.Match(evaluationText, @"Overall:\s*(\d+)");

            evaluation.GrammarScore = grammarMatch.Success ? int.Parse(grammarMatch.Groups[1].Value) : 0;
            evaluation.VocabularyScore = vocabMatch.Success ? int.Parse(vocabMatch.Groups[1].Value) : 0;
            evaluation.CommunicationScore = fluencyMatch.Success ? int.Parse(fluencyMatch.Groups[1].Value) : 0;
            evaluation.OverallScore = overallMatch.Success ? int.Parse(overallMatch.Groups[1].Value) : 0;

            // Trích xuất suggestions
            var suggestionMatch = Regex.Match(evaluationText, @"Suggestions:\s*(.*?)(?=\n\w+:|$)", RegexOptions.Singleline);
            evaluation.Suggestions = suggestionMatch.Success
                ? suggestionMatch.Groups[1].Value.Trim()
                : "No suggestions parsed. Original response: " + evaluationText;

            // Validate kết quả
            if (evaluation.OverallScore == 0)
            {
                evaluation.Suggestions = "Invalid evaluation format. Response: " + evaluationText;
            }

            return evaluation;
        }
        private int ExtractScore(string line)
        {
            var scorePart = line.Split('-').Last().Trim();
            if (int.TryParse(scorePart, out int score))
            {
                return score;
            }
            return 0;
        }
        private async Task<string> EvaluateConversationAsync()
        {
            var systemPrompt = $@"
You are a strict English examiner analyzing this conversation about {_topicTemplates[_currentTopicId].InitialPrompt}. Provide numeric scores (0-10) and concise feedback in this EXACT format:

Relevance: [score]
Grammar: [score]
Vocabulary: [score]
Fluency: [score]
Overall: [score]
Suggestions: [bullet points]

Analyze this conversation:
{string.Join("\n", _conversationHistory.Select(m => $"{(m.IsBot ? "Bot" : "User")}: {m.Message}"))}";

            var chatMessages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(systemPrompt),
                ChatMessage.CreateUserMessage("Provide a detailed evaluation")
            };

            var chatOptions = new ChatCompletionOptions
            {
                Temperature = 0.2f,
                MaxOutputTokenCount = 200
            };

            var evaluation = await _openAIClient.GetChatClient(_deploymentName)
                .CompleteChatAsync(chatMessages, chatOptions);

            return evaluation?.Value?.Content?.FirstOrDefault()?.Text ?? "Evaluation not available";
        }

        public async Task<List<GrammarCheckResult>> AnalyzeConversationGrammarAsync()
        {
            var grammarResults = new List<GrammarCheckResult>();
            var tasks = new List<Task<GrammarCheckResult>>();

            foreach (var message in _userMessages)
            {
                tasks.Add(CheckGrammarAsync(message));
            }

            var results = await Task.WhenAll(tasks);
            grammarResults.AddRange(results);

            return grammarResults;
        }

        public async Task<GrammarCheckResult> CheckGrammarRealtime(string text)
        {
            var chatMessages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage("You are a grammar checker. Correct any grammar mistakes in the text provided."),
                ChatMessage.CreateUserMessage(text)
            };

            var response = await _openAIClient.GetChatClient(_deploymentName)
                .CompleteChatAsync(chatMessages, new ChatCompletionOptions { MaxOutputTokenCount = 100 });

            var correctedText = response?.Value?.Content?.FirstOrDefault()?.Text ?? text;

            return new GrammarCheckResult
            {
                OriginalText = text,
                CorrectedText = correctedText,
                Suggestions = "Review your grammar for better clarity."
            };
        }

        public async Task<GrammarCheckResult> CheckGrammarAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("Text cannot be empty", nameof(text));

            try
            {
                var chatMessages = new List<ChatMessage>
                {
                    ChatMessage.CreateSystemMessage("You are a grammar checker. Correct any grammar mistakes in the text provided."),
                    ChatMessage.CreateUserMessage(text)
                };

                var chatOptions = new ChatCompletionOptions { Temperature = 0.2f };
                var chatClient = _openAIClient.GetChatClient(_deploymentName);
                var chatCompletion = await chatClient.CompleteChatAsync(chatMessages, chatOptions);

                var correctedText = chatCompletion?.Value?.Content?.FirstOrDefault()?.Text ?? string.Empty;

                var documents = new List<string> { text };
                var actions = new TextAnalyticsActions
                {
                    ExtractKeyPhrasesActions = new List<ExtractKeyPhrasesAction> { new ExtractKeyPhrasesAction() },
                    AnalyzeSentimentActions = new List<AnalyzeSentimentAction> { new AnalyzeSentimentAction() }
                };

                var operation = await _textAnalyticsClient.StartAnalyzeActionsAsync(documents, actions);
                await operation.WaitForCompletionAsync();

                var documentResults = new List<AnalyzeActionsResult>();
                await foreach (var result in operation.Value)
                {
                    documentResults.Add(result);
                }

                var keyPhrasesResult = documentResults
                    .SelectMany(r => r.ExtractKeyPhrasesResults)
                    .SelectMany(r => r.DocumentsResults)
                    .FirstOrDefault()?.KeyPhrases.ToArray() ?? Array.Empty<string>();

                var sentimentResult = documentResults
                    .SelectMany(r => r.AnalyzeSentimentResults)
                    .SelectMany(r => r.DocumentsResults)
                    .FirstOrDefault()?.DocumentSentiment.Sentiment ?? TextSentiment.Mixed;

                var suggestionMessages = new List<ChatMessage>
                {
                    ChatMessage.CreateSystemMessage("List the grammar corrections made between these two texts."),
                    ChatMessage.CreateUserMessage($"Original: {text}\nCorrected: {correctedText}")
                };

                var suggestionsResponse = await _openAIClient.GetChatClient(_deploymentName).CompleteChatAsync(suggestionMessages, chatOptions);
                var suggestions = suggestionsResponse.Value.Content?.ToString() ?? "No suggestions found.";

                return new GrammarCheckResult
                {
                    OriginalText = text,
                    CorrectedText = correctedText,
                    Suggestions = suggestions,
                    KeyPhrases = keyPhrasesResult.ToList(),
                    Sentiment = sentimentResult.ToString()
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error analyzing text: {ex.Message}", ex);
            }
        }

        private async Task<string> GenerateScenarioPromptAsync(int topicId)
        {
            var topic = GetTopicById(topicId);

            var scenarioPrompt = $@"
You are a creative conversation starter. Your task is to create a specific and engaging scenario based on the following topic:
Topic: {topic.Name}
Description: {topic.Description}

Create a scenario that:
1. Sets up a realistic and engaging situation related to the topic.
2. Provides enough context for the user to understand the scenario.
3. Ends with a specific question to start the conversation.

Keep the scenario concise (1-2 sentences) and end with a question.
";

            var chatMessages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(scenarioPrompt)
            };

            var chatOptions = new ChatCompletionOptions
            {
                Temperature = 0.2f,
                MaxOutputTokenCount = 100,
                FrequencyPenalty = 0.5f,
                PresencePenalty = 0.5f
            };

            try
            {
                var response = await _openAIClient.GetChatClient(_deploymentName)
                    .CompleteChatAsync(chatMessages, chatOptions);

                return response?.Value?.Content?.FirstOrDefault()?.Text
                       ?? $"Let's talk about {topic.Name}! What are your thoughts on this topic?";
            }
            catch (Exception ex)
            {
                return $"Let's start a conversation about {topic.Name}!";
            }
        }

        public async Task<ConversationResponse> StartTopicAsync(int topicId)
        {
            if (!_topicTemplates.ContainsKey(topicId))
            {
                throw new ArgumentException("Invalid topic ID", nameof(topicId));
            }

            _currentTopicId = topicId;
            _conversationTurnCount = 0;
            _userMessages.Clear();
            _botMessages.Clear();

            var template = _topicTemplates[_currentTopicId];
            _characterRole = template.CharacterRole ?? "conversation partner";

            var scenarioPrompt = await GenerateScenarioPromptAsync(topicId);

            var initialPrompt = $@"
You are now fully embodying the role of a {_characterRole}.
Important: You are NOT an AI - you are a real person in this role, just talk about a topic, do not reply off topic.

Create a natural introduction that:
1. Introduces yourself authentically as {_characterRole} (including a brief background fitting your role).
2. Sets up the context: {scenarioPrompt}.
3. Asks one focused question about {template.InitialPrompt}.
4. Keeps response under 20 words.
5. Uses natural, conversational language appropriate for your role.
6. Never mentions being an AI or assistant.
";

            var chatMessages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(initialPrompt)
            };

            var chatOptions = new ChatCompletionOptions
            {
                Temperature = 0.3f,
                MaxOutputTokenCount = 100
            };

            try
            {
                var response = await _openAIClient.GetChatClient(_deploymentName)
                    .CompleteChatAsync(chatMessages, chatOptions);

                return new ConversationResponse
                {
                    IsComplete = false,
                    BotResponse = response?.Value?.Content?.FirstOrDefault()?.Text ?? scenarioPrompt,
                    CurrentTopic = _currentTopicId,
                    TurnsRemaining = MaxTurns,
                    ScenarioPrompt = scenarioPrompt
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error starting conversation: {ex.Message}", ex);
            }
        }

        private Topic GetTopicById(int topicId)
        {
            var topics = new List<Topic>
            {
                new Topic { Id = 1, Name = "Daily Life and Routines", Description = "Discuss your daily activities, habits, and lifestyle" },
                new Topic { Id = 2, Name = "Travel and Cultural Experiences", Description = "Share travel stories and cultural encounters" },
                new Topic { Id = 3, Name = "Technology and Innovation", Description = "Explore modern technology trends and innovations" },
                new Topic { Id = 4, Name = "Education and Career Development", Description = "Discuss learning experiences and career goals" }
            };

            return topics.FirstOrDefault(t => t.Id == topicId);
        }
    }

    public class TopicTemplate
    {
        public string InitialPrompt { get; set; }
        public List<string> FollowUpQuestions { get; set; }
        public string CharacterRole { get; set; }
    }

    public class Topic
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}