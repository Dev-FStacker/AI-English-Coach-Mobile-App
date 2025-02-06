using BLL.Interface;
using DAL.Entities;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class DistributedCacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;

        public DistributedCacheService(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }

        public async Task<ConversationResponse> GetCachedResponse(string userMessage, int topicId)
        {
            var cacheKey = GenerateCacheKey(userMessage, topicId);
            var cachedData = await _distributedCache.GetStringAsync(cacheKey);
            return cachedData != null
                ? JsonSerializer.Deserialize<ConversationResponse>(cachedData)
                : null;
        }

        public async Task CacheResponse(string userMessage, int topicId, ConversationResponse response)
        {
            var cacheKey = GenerateCacheKey(userMessage, response.CurrentTopic);
            var serializedResponse = JsonSerializer.Serialize(response);
            await _distributedCache.SetStringAsync(cacheKey, serializedResponse,
                new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(30)));
        }

        public Task InvalidateCache(int topicId)
        {
            // Implement distributed cache invalidation logic
            return Task.CompletedTask;
        }

        private string GenerateCacheKey(string userMessage, int topicId)
        {
            return $"Conversation_Topic{topicId}_{userMessage.GetHashCode()}";
        }
    }
}
