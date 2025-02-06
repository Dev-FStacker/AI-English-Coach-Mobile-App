    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using BLL.Interface;
    using Microsoft.AspNetCore.Http;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    namespace BLL.Services
    {
        public class AzureStorageService : IAzureStorageService
        {
            private readonly string _storageAccount = "stbennsse184260165107142";
            private readonly string _containerName = "talkai";
            private readonly string _sasToken = "sp=racwdli&si=Tri&spr=https&sv=2022-11-02&sr=c&sig=a5t9qIF5tR1yxsaciChCAVSgi1yNHbYU13hM%2BbnP2Dc%3D";

            public async Task<string> GetAudioFileUrl(string fileName)
            {
                var baseurl = $"https://{_storageAccount}.blob.core.windows.net/{_containerName}/{fileName}";
                return baseurl + "?" + _sasToken;
            }

            public async Task<string> UploadAudioFile(byte[] audioData)
            {
                var fileName = $"{Guid.NewGuid()}.wav";
                var containerUri = $"https://{_storageAccount}.blob.core.windows.net/{_containerName}?{_sasToken}";

                var containerClient = new BlobContainerClient(new Uri(containerUri));
                var blobClient = containerClient.GetBlobClient(fileName);

                using var stream = new MemoryStream(audioData)
                {
                    Position = 0
                };

                await blobClient.UploadAsync(stream);
                return fileName;
            }
    

        public async Task<List<string>> ListAudioFiles()
            {
                try
                {
                    var containerUri = $"https://{_storageAccount}.blob.core.windows.net/{_containerName}?{_sasToken}";
                    var containerClient = new BlobContainerClient(new Uri(containerUri));
                    var blobs = new List<string>();

                    await foreach (var blob in containerClient.GetBlobsAsync())
                    {
                        if (blob.Name.EndsWith(".wav"))
                        {
                            blobs.Add(blob.Name);
                        }
                    }

                    return blobs;
                }

                catch (Exception ex)
                {
                    throw new Exception($"Error listing audio files: {ex.Message}", ex);
                }
            }
            public async Task<string> GetLatestAudioFileName(string fileName)
            {
                try
                {
                    var containerUri = $"https://{_storageAccount}.blob.core.windows.net/{_containerName}?{_sasToken}";
                    var containerClient = new BlobContainerClient(new Uri(containerUri));

                    var blobs = new List<BlobItem>();
                    await foreach (var blob in containerClient.GetBlobsAsync())
                    {
                        if (blob.Name.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                        {
                            blobs.Add(blob);
                        }
                    }

                    if (blobs.Count == 0)
                    {
                        throw new Exception("Không tìm thấy file audio nào trong container.");
                    }


                    var latestBlob = blobs
                        .OrderByDescending(b => b.Properties.LastModified)
                        .First();

                    return latestBlob.Name;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Lỗi khi lấy file mới nhất: {ex.Message}", ex);
                }
            }
        public async Task<byte[]> GetAudioBytes(string fileName)
        {
          
            var containerUri = $"https://{_storageAccount}.blob.core.windows.net/{_containerName}?{_sasToken}";
            var containerClient = new BlobContainerClient(new Uri(containerUri));
            var blobClient = containerClient.GetBlobClient(fileName);

            using var memoryStream = new MemoryStream();
            await blobClient.DownloadToAsync(memoryStream);
            return memoryStream.ToArray(); // ✅ Trả về byte[]
        }
    }
    }