using TheLoot.Services;
using Microsoft.AspNetCore.Mvc;

namespace TheLoot.Controllers
{
    [ApiController]
    [Route("api/plaid")]
    public class PlaidController : ControllerBase
    {
        private readonly PlaidService _plaidService;

        public PlaidController(PlaidService plaidService)
        {
            _plaidService = plaidService;
        }

        [HttpGet("link-token")]
        public async Task<IActionResult> GetLinkToken([FromQuery] string userId)
        {
            var linkToken = await _plaidService.CreateLinkTokenAsync(userId);
            return Ok(new { linkToken });
        }

        [HttpPost("exchange-token")]
        public async Task<IActionResult> ExchangeToken([FromBody] ExchangeTokenRequest request)
        {
            var accessToken = await _plaidService.ExchangePublicTokenAsync(request.PublicToken);
            return Ok(new { accessToken });
        }

        public class ExchangeTokenRequest
        {
            public string PublicToken { get; set; }
        }
    }

}
