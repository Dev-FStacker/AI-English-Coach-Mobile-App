using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Interface
{
    public interface ICacheService
    {
        Task<ConversationResponse> GetCachedResponse(string userMessage, int topicId);
        Task CacheResponse(string userMessage, int topicId, ConversationResponse response);
        Task InvalidateCache(int topicId);
    }
}
