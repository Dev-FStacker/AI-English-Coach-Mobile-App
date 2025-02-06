using BLL.Interface;
using BLL.Interfaces;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    // Hubs/ChatHub.cs
    public class ChatHub : Hub
    {
        private readonly IAzureLanguageService _languageService;
        private readonly IAzureSpeechService _conversationService;

        public ChatHub(
            IAzureLanguageService languageService,
            IAzureSpeechService conversationService)
        {
            _languageService = languageService;
            _conversationService = conversationService;
        }

        public async Task SendMessage(string message)
        {
            var userId = Guid.NewGuid();
            // Xử lý tin nhắn với AI
            var response = await _languageService.ProcessConversationAsync(message);

            // Lưu cuộc trò chuyện
            await _conversationService.SaveConversation(userId, null, message);

            // Gửi phản hồi về client
            await Clients.Caller.SendAsync("ReceiveMessage", new
            {
                message = response.BotResponse,
                isBot = true,
                timestamp = DateTime.UtcNow
            });
        }
        public async Task SendAudio(string user, string audioBase64)
        {
            await Clients.All.SendAsync("ReceiveAudio", user, audioBase64);
        }
    }
}
