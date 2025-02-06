using BLL.Interface;
using DAL.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(30);
        private readonly ConcurrentDictionary<int, List<string>> _topicKeys = new();

        public CacheService(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }

        public async Task<ConversationResponse> GetCachedResponse(string userMessage, int topicId)
        {
            var cacheKey = GenerateCacheKey(userMessage, topicId);
            try
            {
                var cachedData = await _distributedCache.GetStringAsync(cacheKey);
                return cachedData != null ? JsonConvert.DeserializeObject<ConversationResponse>(cachedData) : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing cached response: {ex.Message}");
                return null;
            }
        }

        public async Task CacheResponse(string userMessage, int topicId, ConversationResponse response)
        {
            var cacheKey = GenerateCacheKey(userMessage, topicId);
            try
            {
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _defaultExpiration
                };
                await _distributedCache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(response), cacheOptions);

                // Lưu key vào danh sách theo topicId
                _topicKeys.AddOrUpdate(topicId, new List<string> { cacheKey }, (key, existingList) =>
                {
                    existingList.Add(cacheKey);
                    return existingList;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error caching response: {ex.Message}");
            }
        }

        public Task InvalidateCache(int topicId)
        {
            if (_topicKeys.TryGetValue(topicId, out var keys))
            {
                foreach (var key in keys)
                {
                    _distributedCache.RemoveAsync(key);
                }
                _topicKeys.TryRemove(topicId, out _);
            }
            return Task.CompletedTask;
        }

        private string GenerateCacheKey(string userMessage, int topicId)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(userMessage));
            var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            return $"Conversation_Topic{topicId}_{hashString}";
        }
    }
}