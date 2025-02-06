using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Media;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace LanguageCoachApp
{
    public partial class MainPage : ContentPage
    {
        private readonly ISpeechToText speechToText;
        private CancellationTokenSource tokenSource;
        public string RecognitionText { get; set; } = string.Empty;
        public CultureInfo culture { get; set; } = CultureInfo.CurrentCulture;

        public MainPage() : this(SpeechToText.Default) { }

        public MainPage(ISpeechToText speechToText)
        {
            InitializeComponent();
            this.speechToText = speechToText;
            BindingContext = this;
        }

        public async void Listen(object sender, EventArgs args)
        {
            // Create a new CancellationTokenSource each time you start listening
            tokenSource = new CancellationTokenSource();

            try
            {
                var isAuthorized = await speechToText.RequestPermissions(tokenSource.Token);
                if (!isAuthorized)
                {
                    await DisplayAlert("Permission Error", "Microphone access denied.", "OK");
                    return;
                }

                RecognitionText = "";
                OnPropertyChanged(nameof(RecognitionText));

                // *** KEY CHANGE: Use a try-catch INSIDE the ListenAsync call ***
                try
                {
                    var text = await speechToText.ListenAsync(
                        culture, // Use the culture property
                        new Progress<string>(partialText =>
                        {
                            if (!tokenSource.IsCancellationRequested) // Check for cancellation
                            {
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    RecognitionText += partialText + " ";
                                    OnPropertyChanged(nameof(RecognitionText));
                                });
                            }
                        }),
                        tokenSource.Token);


                    if (!string.IsNullOrWhiteSpace(text.ToString())) // Check if text is not null or empty
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            RecognitionText += text;
                            OnPropertyChanged(nameof(RecognitionText));
                        });
                    }
                    else if (!tokenSource.IsCancellationRequested) // Handle "no speech" only if not cancelled
                    {
                        MainThread.BeginInvokeOnMainThread(() => {
                            RecognitionText = "No speech detected.";
                            OnPropertyChanged(nameof(RecognitionText));
                        });
                    }
                }
                catch (Exception ex)
                {
                    // Handle exceptions from ListenAsync specifically
                    if (!(ex is OperationCanceledException)) // Don't report cancellation as an error
                    {
                        await DisplayAlert("Speech Recognition Error", $"Error during recognition: {ex.Message}", "OK");
                    }
                }


            }
            catch (OperationCanceledException)
            {
                // This is expected when the user cancels, no need to display a toast.
                //await Toast.Make("Listening was canceled").Show(CancellationToken.None);  REMOVE THIS
            }
            catch (Exception ex)
            {
                await DisplayAlert("General Error", $"An unexpected error occurred: {ex.Message}", "OK"); // More general error handling
            }
        }


        public void StopListening(object sender, EventArgs args)
        {
            tokenSource?.Cancel();
        }
    }
}