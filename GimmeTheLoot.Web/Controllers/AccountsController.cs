using GimmeTheLoot.Shared.Models.DTO;
using GimmeTheLoot.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GimmeTheLoot.Web.Controllers
{
    [ApiController]
    [Route("api/accounts")]
    [Authorize]
    public class AccountsController: ControllerBase
    {
        private readonly AppDbContext _db;

        public AccountsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("user-accounts")]
        public async Task<IActionResult> GetUserAccounts()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found in token.");

            var userAccounts = await _db.PlaidAccounts
                .Include(a => a.UserPlaidAccount)
                .Where(a => a.UserPlaidAccount.UserId == userId)
                .ToListAsync();
            return Ok(userAccounts);
        }

        [HttpGet("user-transactions")]
        public async Task<IActionResult> GetUserTransactions()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var transactions = await _db.Transactions
                .Include(t => t.TransactionCategory)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            return Ok(transactions);
        }

    }
}
