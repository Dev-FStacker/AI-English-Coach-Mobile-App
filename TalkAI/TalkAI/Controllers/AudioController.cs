using BLL.Interface;
using BLL.Interfaces;
using BLL.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TalkAI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AudioController : ControllerBase
    {
        private readonly IAzureStorageService _storageService;
        private readonly IAzureSpeechService _speechService;
        private readonly ILogger<AudioController> _logger;

        public AudioController(
            IAzureStorageService storageService,
            IAzureSpeechService speechService,
            ILogger<AudioController> logger)
        {
            _storageService = storageService;
            _speechService = speechService;
            _logger = logger;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetAudioFiles()
        {
            try
            {
                var files = await _storageService.ListAudioFiles();
                return Ok(new { files });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audio files list");
                return StatusCode(500, "Failed to retrieve audio files");
            }
        }

        [HttpPost("convert-to-text/{fileName}")]
        public async Task<IActionResult> ConvertAudioToText(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    return BadRequest("File name is required");
                }

                // Get the full URL with SAS token for the specific file
                var fileUrl = await _storageService.GetAudioFileUrl(fileName);

                // Convert to text using Speech Service
                var text = await _speechService.ConvertSpeechToTextFromBlobStorage(fileUrl);

                // Save conversation
                var userId  = Guid.NewGuid();
                await _speechService.SaveConversation(userId, fileName, text);

                return Ok(new
                {
                    fileName,
                    text,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting audio to text for file: {FileName}", fileName);
                return StatusCode(500, "Failed to convert audio to text");
            }
        }
      
        [HttpGet("file/{fileName}")]
        public async Task<IActionResult> GetAudioFileUrl(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    return BadRequest("File name is required");
                }

                var fileUrl = await _storageService.GetAudioFileUrl(fileName);
                return Ok(new { url = fileUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting URL for file: {FileName}", fileName);
                return StatusCode(500, "Failed to get file URL");
            }
        }

        [HttpPost("process-audio")]
        public async Task<IActionResult> ProcessAudioFile([FromBody] ProcessAudioRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.FileName))
                {
                    return BadRequest("File name is required");
                }

                // Get audio file URL
                var fileUrl = await _storageService.GetAudioFileUrl(request.FileName);

                // Convert to text
                var text = await _speechService.ConvertSpeechToTextFromBlobStorage(fileUrl);

                // Save conversation
                var userId = Guid.NewGuid();
                var saved = await _speechService.SaveConversation(userId, request.FileName, text);

                if (!saved)
                {
                    _logger.LogWarning("Failed to save conversation for file: {FileName}", request.FileName);
                }

                return Ok(new
                {
                    fileName = request.FileName,
                    text,
                    saved,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing audio file: {FileName}", request.FileName);
                return StatusCode(500, "Failed to process audio file");
            }
        }
    }

    public class ProcessAudioRequest
    {
        public string FileName { get; set; }
    }
}
