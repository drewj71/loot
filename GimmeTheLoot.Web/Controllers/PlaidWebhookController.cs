using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using GimmeTheLoot.Web.Services;

namespace GimmeTheLoot.Web.Controllers
{
    [ApiController]
    [Route("api/plaid-webhook")]
    public class PlaidWebhookController : ControllerBase
    {
        private readonly ILogger<PlaidWebhookController> _logger;
        private readonly PlaidService _plaidService;

        public PlaidWebhookController(
            ILogger<PlaidWebhookController> logger,
            PlaidService plaidService)
        {
            _logger = logger;
            _plaidService = plaidService;
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveWebhook()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            _logger.LogInformation("Received Plaid webhook: {WebhookBody}", body);

            JsonDocument jsonDoc;
            try
            {
                jsonDoc = JsonDocument.Parse(body);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid JSON webhook body.");
                return BadRequest("Invalid JSON");
            }

            var root = jsonDoc.RootElement;

            var webhookType = root.GetProperty("webhook_type").GetString();
            var webhookCode = root.GetProperty("webhook_code").GetString();
            var itemId = root.GetProperty("item_id").GetString();

            _logger.LogInformation("Webhook Type: {WebhookType}, Code: {WebhookCode}, ItemId: {ItemId}",
                webhookType, webhookCode, itemId);

            if (webhookType == "TRANSACTIONS")
            {
                switch (webhookCode)
                {
                    case "INITIAL_UPDATE":
                    case "HISTORICAL_UPDATE":
                    case "DEFAULT_UPDATE":
                        try
                        {
                            await _plaidService.SyncTransactionsForItemAsync(itemId);
                            _logger.LogInformation("Synced transactions for item {ItemId}", itemId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error syncing transactions for item {ItemId}", itemId);
                        }
                        break;

                    case "TRANSACTIONS_REMOVED":
                        var removed = root.GetProperty("removed_transactions").EnumerateArray().Select(t => t.GetString()).ToList();
                        _logger.LogInformation("Transactions removed: {RemovedTransactions}", string.Join(", ", removed));
                        // Optionally delete from your DB
                        break;

                    default:
                        _logger.LogInformation("Unhandled webhook code: {WebhookCode}", webhookCode);
                        break;
                }
            }

            return Ok();
        }
    }
}
