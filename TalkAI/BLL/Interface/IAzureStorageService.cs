using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Interface
{
    public interface IAzureStorageService
    {
        Task<string> GetAudioFileUrl(string fileName);
        Task<List<string>> ListAudioFiles();
        Task<string> UploadAudioFile(byte[] audioData);
        Task<string> GetLatestAudioFileName(string fileName);
        Task<byte[]> GetAudioBytes(string fileName);

    }
}
