using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Media;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using Microsoft.Maui.Media;

namespace LanguageCoachApp
{
    public class MessageModel
    {
        public string Text { get; set; }
        public bool IsUser { get; set; }
    }

    public partial class MainPage : ContentPage
    {
        private readonly ISpeechToText speechToText;
        private readonly HttpClient httpClient = new HttpClient();
        private CancellationTokenSource tokenSource;
        private const string ApiKey = "AIzaSyDAo4O_Cze7lx9PpBQu9GvjrrlbP37HK9Y";
        public string RecognitionText { get; set; } = string.Empty;
        public string AIResponseText { get; set; } = string.Empty;
        public ObservableCollection<MessageModel> Messages { get; set; } = new ObservableCollection<MessageModel>();
        public CultureInfo culture { get; set; } = CultureInfo.CurrentCulture;
        public MainPage() : this(SpeechToText.Default) { }

        public MainPage(ISpeechToText speechToText)
        {
            InitializeComponent();
            this.speechToText = speechToText;
            BindingContext = this;
            AnimateUI();
        }

        private async void AnimateUI()
        {
            await Task.Delay(500);
            await MainContainer.FadeTo(1, 1000);
            await TitleLabel.FadeTo(1, 1000);
            await SubtitleLabel.FadeTo(1, 1000);
            await SpeechFrame.FadeTo(1, 1000);
            await StartButton.FadeTo(1, 1000);
        }

        public async void Listen(object sender, EventArgs args)
        {
            tokenSource = new CancellationTokenSource();
            try
            {
                var isAuthorized = await speechToText.RequestPermissions(tokenSource.Token);
                if (!isAuthorized)
                {
                    await DisplayAlert("Permission Error", "Microphone access denied.", "OK");
                    return;
                }

                var result = await speechToText.ListenAsync(CultureInfo.CurrentCulture, null, tokenSource.Token);

                if (result.IsSuccessful && !string.IsNullOrWhiteSpace(result.Text))
                {
                    Messages.Add(new MessageModel { Text = "You: " + result.Text, IsUser = true });
                    await ScrollToBottom();
                    await SendToGemini(result.Text);
                }
                else
                {
                    Messages.Add(new MessageModel { Text = "No speech detected.", IsUser = true });
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async Task SendToGemini(string text)
        {
            try
            {
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text }
                            }
                        }
                    }
                };

                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={ApiKey}";
                var response = await httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();

                    var jsonResponse = JsonConvert.DeserializeObject<GeminiResponse>(responseBody);

                    string aiResponseText = jsonResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text ?? "No response received.";
                    Messages.Add(new MessageModel { Text = "Gemini: " + aiResponseText, IsUser = false });
                    await ScrollToBottom();
                    var locales = await TextToSpeech.GetLocalesAsync();
                    var selectedLocale = locales.FirstOrDefault(l => l.Language.StartsWith("en"));
                    var speechOptions = new SpeechOptions()
                    {
                        Locale = selectedLocale ?? locales.FirstOrDefault()
                    };
                    await TextToSpeech.SpeakAsync(aiResponseText, speechOptions);
                }
                else
                {
                    Messages.Add(new MessageModel { Text = "API Error: " + response.StatusCode, IsUser = false });
                    await ScrollToBottom();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("API Error", $"An error occurred: {ex.Message}", "OK");
            }
        }
        public async void SendTextMessage(object sender, EventArgs args)
        {
            string userText = UserInput.Text.Trim();
            if (!string.IsNullOrWhiteSpace(userText))
            {
                Messages.Add(new MessageModel { Text = "You: " + userText, IsUser = true });
                UserInput.Text = "";
                await SendToGemini(userText);
                await ScrollToBottom();
            }
        }
        private async Task ScrollToBottom()
        {
            await Task.Delay(100);
            if (Messages.Count > 0)
            {
                MessagesCollectionView.ScrollTo(Messages.Last(), position: ScrollToPosition.End, animate: true);
            }
        }


        public class GeminiResponse
        {
            [JsonProperty("candidates")]
            public List<Candidate> Candidates { get; set; }
        }

        public class Candidate
        {
            [JsonProperty("content")]
            public Content Content { get; set; }
        }

        public class Content
        {
            [JsonProperty("parts")]
            public List<Part> Parts { get; set; }
        }

        public class Part
        {
            [JsonProperty("text")]
            public string Text { get; set; }
        }

    }
}
