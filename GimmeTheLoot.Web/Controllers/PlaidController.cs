using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GimmeTheLoot.Web.Services;
using System.Security.Claims;

namespace GimmeTheLoot.Web.Controllers
{
    [ApiController]
    [Route("api/plaid")]
    [Authorize]
    public class PlaidController : ControllerBase
    {
        private readonly PlaidService _plaidService;
        private readonly ILogger<PlaidController> _logger;

        public PlaidController(PlaidService plaidService, ILogger<PlaidController> logger)
        {
            _plaidService = plaidService;
            _logger = logger;
        }

        [HttpPost("link-token")]
        public async Task<IActionResult> CreateLinkToken()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found in token.");

            try
            {
                var linkToken = await _plaidService.CreateLinkTokenAsync(userId);
                return Ok(new { link_token = linkToken });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Plaid Link Token for {UserId}", userId);
                return Problem(
                    detail: ex.Message,
                    title: "Plaid API Error",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        [HttpPost("exchange-public-token")]
        public async Task<IActionResult> ExchangePublicToken([FromBody] PublicTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PublicToken))
                return BadRequest("Public token is required.");

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found.");

                // Exchange the public token for access token and item ID
                var (accessToken, itemId) = await _plaidService.ExchangePublicTokenAsync(request.PublicToken);

                // Store the access token and item ID in your database linked to the user
                await _plaidService.StorePlaidItemAsync(userId, accessToken, itemId);

                // **Sync the individual accounts for this item now**
                await _plaidService.SyncPlaidAccountsAsync(userId, accessToken, itemId);

                return Ok(new { access_token = accessToken, item_id = itemId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exchanging public token for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                return Problem(
                    detail: ex.Message,
                    title: "Plaid API Error",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }



        [HttpPost("accounts")]
        public async Task<IActionResult> GetAccounts([FromBody] AccessTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.AccessToken))
                return BadRequest("Access token is required.");

            try
            {
                var accounts = await _plaidService.GetAccountsAsync(request.AccessToken);
                return Ok(accounts.Accounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accounts");
                return Problem(
                    detail: ex.Message,
                    title: "Plaid API Error",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        [HttpPost("transactions")]
        public async Task<IActionResult> GetTransactions([FromBody] TransactionsRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.AccessToken))
                return BadRequest("Access token is required.");

            try
            {
                var transactions = await _plaidService.GetTransactionsAsync(
                    request.AccessToken,
                    DateOnly.FromDateTime(request.StartDate),
                    DateOnly.FromDateTime(request.EndDate)
                );

                return Ok(transactions.Transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transactions");
                return Problem(
                    detail: ex.Message,
                    title: "Plaid API Error",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        [HttpPost("refresh-balances")]
        public async Task<IActionResult> RefreshBalances()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                await _plaidService.RefreshUserAccountBalancesAsync(userId);
                return Ok(new { message = "Balances refreshed." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh balances for user {UserId}", userId);
                return StatusCode(500, "Error refreshing balances.");
            }
        }

        [HttpPost("sync-transactions")]
        public async Task<IActionResult> SyncTransactions()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var transactions = await _plaidService.SyncTransactionsForUserAsync(userId);

            return Ok(transactions);
        }

        public class PublicTokenRequest
        {
            public string PublicToken { get; set; } = string.Empty;
        }

        public class AccessTokenRequest
        {
            public string AccessToken { get; set; } = string.Empty;
        }

        public class TransactionsRequest
        {
            public string AccessToken { get; set; } = string.Empty;
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public int Count { get; set; } = 100;
            public int Offset { get; set; } = 0;
        }
    }
}
