using BLL.Interface;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class GrammarController : ControllerBase
{
    private readonly IAzureLanguageService _languageService;
    private readonly ILogger<GrammarController> _logger;

    public GrammarController(
        IAzureLanguageService languageService,
        ILogger<GrammarController> logger)
    {
        _languageService = languageService;
        _logger = logger;
    }

    [HttpPost("check")]
    public async Task<IActionResult> CheckGrammar([FromBody] GrammarCheckRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Text))
                return BadRequest("Text is required");

            var result = await _languageService.CheckGrammarAsync(request.Text);

            return Ok(new
            {
                originalText = result.OriginalText,
                correctedText = result.CorrectedText,
                suggestions = result.Suggestions,
                keyPhrases = result.KeyPhrases,
                sentiment = result.Sentiment
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking grammar");
            return StatusCode(500, "An error occurred while checking grammar");
        }
    }

    // Thêm endpoint để check grammar realtime trong conversation
    [HttpPost("check-realtime")]
    public async Task<IActionResult> CheckGrammarRealtime([FromBody] GrammarCheckRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Text))
                return BadRequest("Text is required");

            var result = await _languageService.CheckGrammarRealtime(request.Text);

            // Trả về kết quả ngắn gọn hơn cho realtime checking
            return Ok(new
            {
                corrections = GetMainCorrections(result.CorrectedText, result.OriginalText),
                suggestions = GetConciseSuggestions(result.Suggestions)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking grammar realtime");
            return StatusCode(500, "An error occurred while checking grammar");
        }
    }

    private string GetMainCorrections(string corrected, string original)
    {
        // Logic để so sánh và trả về các sửa đổi chính
        if (corrected == original) return "No corrections needed";
        return corrected;
    }

    private string GetConciseSuggestions(string fullSuggestions)
    {
        // Logic để rút gọn suggestions
        if (string.IsNullOrEmpty(fullSuggestions)) return null;
        return fullSuggestions.Split('.').FirstOrDefault();
    }
}

    public class GrammarCheckRequest
{
    public string Text { get; set; }
}