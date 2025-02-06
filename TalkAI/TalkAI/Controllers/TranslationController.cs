using BLL.Interface;
using Common.DTO;
using DAL.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TalkAI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TranslationController : ControllerBase
    {
        private readonly ITranslationService _translationService;

        public TranslationController(ITranslationService translationService)
        {
            _translationService = translationService;
        }

        [HttpPost("translate")]
        public async Task<ActionResult<TranslationResponse>> TranslateConversation(
            [FromBody] TranslationRequest request)
        {
            try
            {
                var translation = await _translationService.TranslateTextAsync(
                    request.Message,
                    request.TargetLanguage,
                    request.SourceLanguage
                );

                var response = new TranslationResponse
                {
                    Message = translation.TranslatedText, // Đây là văn bản đã được dịch
                    TargetLanguage = request.TargetLanguage,
                    SourceLanguage = request.SourceLanguage
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class TranslationResponse
    {
        public string Message { get; set; }
        public string TargetLanguage { get; set; }
        public string SourceLanguage { get; set; }
    }
}
