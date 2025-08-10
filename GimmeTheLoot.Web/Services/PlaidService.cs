using GimmeTheLoot.Shared.Models.Entity;
using GimmeTheLoot.Web.Data;
using Going.Plaid;
using Going.Plaid.Accounts;
using Going.Plaid.Entity;
using Going.Plaid.Item;
using Going.Plaid.Link;
using Going.Plaid.Transactions;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace GimmeTheLoot.Web.Services
{
    public class PlaidService
    {
        private readonly PlaidClient _plaidClient;
        private readonly AppDbContext _db;
        private readonly CacheService _cacheService;

        public PlaidService(PlaidClient plaidClient, AppDbContext db, CacheService cacheService)
        {
            _plaidClient = plaidClient;
            _db = db;
            _cacheService = cacheService;
        }

        public async Task<string> CreateLinkTokenAsync(string userId)
        {
            var response = await _plaidClient.LinkTokenCreateAsync(new LinkTokenCreateRequest
            {
                ClientName = "Loot",
                Language = Language.English,
                CountryCodes = [CountryCode.Us],
                User = new LinkTokenCreateRequestUser { ClientUserId = userId },
                Products = [Products.Auth, Products.Transactions]
            });

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Plaid error: {response.Error}");

            return response.LinkToken;
        }

        public async Task<(string AccessToken, string ItemId)> ExchangePublicTokenAsync(string publicToken)
        {
            var response = await _plaidClient.ItemPublicTokenExchangeAsync(new ItemPublicTokenExchangeRequest
            {
                PublicToken = publicToken
            });

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Plaid token exchange failed: {response.Error}");

            return (response.AccessToken, response.ItemId);
        }

        public async Task<AccountsGetResponse> GetAccountsAsync(string accessToken)
        {
            var response = await _plaidClient.AccountsGetAsync(new AccountsGetRequest
            {
                AccessToken = accessToken
            });

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Plaid accounts failed: {response.Error}");

            return response;
        }

        public async Task<TransactionsGetResponse> GetTransactionsAsync(string accessToken, DateOnly start, DateOnly end)
        {
            var response = await _plaidClient.TransactionsGetAsync(new TransactionsGetRequest
            {
                AccessToken = accessToken,
                StartDate = start,
                EndDate = end
            });

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Plaid transactions failed: {response.Error}");

            return response;
        }

        public async Task SyncTransactionsForItemAsync(string itemId)
        {
            var userPlaidAccount = await _db.UserPlaidAccounts
                .Include(upa => upa.Accounts)
                .FirstOrDefaultAsync(upa => upa.ItemId == itemId);

            if (userPlaidAccount == null)
            {
                throw new Exception($"No user Plaid account found for item ID: {itemId}");
            }

            var accessToken = userPlaidAccount.AccessToken;

            var request = new TransactionsGetRequest
            {
                AccessToken = accessToken,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Options = new TransactionsGetRequestOptions
                {
                    Count = 100,
                    Offset = 0
                }
            };

            var response = await _plaidClient.TransactionsGetAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to fetch transactions: {response.Error?.ErrorMessage}");
            }

            var plaidTransactions = response.Transactions;

            // Load existing transaction IDs to deduplicate
            var existingTransactionIds = await _db.Transactions
                .Where(t => t.UserId == userPlaidAccount.UserId)
                .Select(t => t.TransactionId)
                .ToListAsync();

            var newTransactions = new List<GimmeTheLoot.Shared.Models.Entity.Transaction>();

            foreach (var tx in plaidTransactions)
            {
                if (existingTransactionIds.Contains(tx.TransactionId)) continue;

                newTransactions.Add(new GimmeTheLoot.Shared.Models.Entity.Transaction
                {
                    UserId = userPlaidAccount.UserId,
                    AccountId = tx.AccountId,
                    Date = (DateTimeOffset)tx.Datetime,
                    Name = tx.Name,
                    Amount = (decimal)tx.Amount,
                    Category = tx.Category?.FirstOrDefault() ?? "Uncategorized",
                    TransactionId = tx.TransactionId
                });
            }

            if (newTransactions.Any())
            {
                _db.Transactions.AddRange(newTransactions);
                await _db.SaveChangesAsync();
            }
        }

        public async Task StorePlaidItemAsync(string userId, string accessToken, string itemId)
        {
            var existing = await _db.UserPlaidAccounts.FirstOrDefaultAsync(u => u.UserId == userId && u.ItemId == itemId);
            if (existing == null)
            {
                var newAccount = new UserPlaidAccount
                {
                    UserId = userId,
                    AccessToken = accessToken,
                    ItemId = itemId,
                    ConnectedOn = DateTime.UtcNow
                };
                _db.UserPlaidAccounts.Add(newAccount);
                await _db.SaveChangesAsync();
            }
            else
            {
                // Optionally update tokens if needed
                existing.AccessToken = accessToken;
                await _db.SaveChangesAsync();
            }
        }

        public async Task SyncPlaidAccountsAsync(string userId, string accessToken, string itemId)
        {
            // Fetch UserPlaidAccount entity from DB
            var userPlaidAccount = await _db.UserPlaidAccounts.FirstOrDefaultAsync(u => u.UserId == userId && u.ItemId == itemId);
            if (userPlaidAccount == null)
                throw new Exception("UserPlaidAccount not found");

            // Get accounts from Plaid
            var response = await _plaidClient.AccountsGetAsync(new AccountsGetRequest
            {
                AccessToken = accessToken
            });

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Plaid AccountsGet failed: {response.Error}");

            var plaidAccounts = response.Accounts;

            // Remove or update existing accounts and add new ones
            var existingAccounts = await _db.PlaidAccounts.Where(a => a.UserPlaidAccountId == userPlaidAccount.Id).ToListAsync();

            foreach (var account in plaidAccounts)
            {
                var existing = existingAccounts.FirstOrDefault(a => a.AccountId == account.AccountId);
                if (existing != null)
                {
                    // Update existing
                    existing.Name = account.Name;
                    existing.OfficialName = account.OfficialName;
                    existing.Type = account.Type.ToString();
                    existing.Subtype = account.Subtype.ToString();
                    existing.CurrentBalance = (decimal)(account.Balances?.Current ?? 0);
                    existing.AvailableBalance = (decimal)(account.Balances?.Available ?? 0);
                }
                else
                {
                    // Add new
                    _db.PlaidAccounts.Add(new PlaidAccount
                    {
                        AccountId = account.AccountId,
                        Name = account.Name,
                        OfficialName = account.OfficialName,
                        Type = account.Type.ToString(),
                        Subtype = account.Subtype.ToString(),
                        CurrentBalance = (decimal)(account.Balances?.Current ?? 0),
                        AvailableBalance = (decimal)(account.Balances?.Available ?? 0),
                        UserPlaidAccountId = userPlaidAccount.Id
                    });
                }
            }

            // Optional: Remove accounts no longer returned by Plaid (accounts removed by user)
            var plaidAccountIds = plaidAccounts.Select(a => a.AccountId).ToHashSet();
            var toRemove = existingAccounts.Where(a => !plaidAccountIds.Contains(a.AccountId)).ToList();
            if (toRemove.Any())
            {
                _db.PlaidAccounts.RemoveRange(toRemove);
            }

            await _db.SaveChangesAsync();
        }

        public async Task RefreshUserAccountBalancesAsync(string userId)
        {
            // Get the user's linked Plaid accounts with access tokens
            var userPlaidAccounts = await _db.UserPlaidAccounts
                .Include(upa => upa.Accounts)
                .Where(upa => upa.UserId == userId)
                .ToListAsync();

            foreach (var userPlaidAccount in userPlaidAccounts)
            {
                // Call Plaid to get latest accounts info
                var response = await _plaidClient.AccountsGetAsync(new AccountsGetRequest
                {
                    AccessToken = userPlaidAccount.AccessToken
                });

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Plaid AccountsGet failed: {response.Error?.ErrorMessage}");
                }

                // Update each stored PlaidAccount with fresh balance info
                foreach (var plaidAccount in response.Accounts)
                {
                    var existingAccount = userPlaidAccount.Accounts.FirstOrDefault(a => a.AccountId == plaidAccount.AccountId);
                    if (existingAccount != null)
                    {
                        existingAccount.CurrentBalance = (decimal)plaidAccount.Balances.Current;
                        existingAccount.AvailableBalance = (decimal?)plaidAccount.Balances.Available ?? 0m;
                        existingAccount.Name = plaidAccount.Name;
                        existingAccount.OfficialName = plaidAccount.OfficialName ?? "";
                        existingAccount.Type = plaidAccount.Type.ToString();
                        existingAccount.Subtype = plaidAccount.Subtype.ToString();
                    }
                    else
                    {
                        // New account found (optional: add new accounts if you want)
                        userPlaidAccount.Accounts.Add(new PlaidAccount
                        {
                            AccountId = plaidAccount.AccountId,
                            Name = plaidAccount.Name,
                            OfficialName = plaidAccount.OfficialName ?? "",
                            Type = plaidAccount.Type.ToString(),
                            Subtype = plaidAccount.Subtype.ToString(),
                            CurrentBalance = (decimal)plaidAccount.Balances.Current,
                            AvailableBalance = (decimal?)plaidAccount.Balances.Available ?? 0m
                        });
                    }
                }
            }

            await _db.SaveChangesAsync();
        }

        public async Task<List<GimmeTheLoot.Shared.Models.Entity.Transaction>> SyncTransactionsForUserAsync(string userId)
        {
            var categoryCache = await _cacheService.GetOrLoadCategoryCacheAsync();

            var newCategories = new List<TransactionCategory>();
            var allTransactions = new List<GimmeTheLoot.Shared.Models.Entity.Transaction>();

            // Get all linked accounts for the user
            var accounts = await _db.UserPlaidAccounts
                .Where(a => a.UserId == userId)
                .ToListAsync();

            foreach (var account in accounts)
            {
                var plaidResponse = await _plaidClient.TransactionsGetAsync(new TransactionsGetRequest
                {
                    AccessToken = account.AccessToken,
                    StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-3).Date),
                    EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date)
                });

                if (!plaidResponse.IsSuccessStatusCode)
                    continue;

                var plaidTransactions = plaidResponse.Transactions;

                var existingTxIds = await _db.Transactions
                    .Where(t => t.AccountId == account.Id.ToString())
                    .Select(t => t.TransactionId)
                    .ToListAsync();

                foreach (var pt in plaidTransactions)
                {
                    if (existingTxIds.Contains(pt.TransactionId))
                        continue;

                    var primary = (pt.PersonalFinanceCategory != null)
                        ? pt.PersonalFinanceCategory.Primary
                        : "UNCATEGORIZED";

                    var key = (pt.PersonalFinanceCategory != null)
                        ? pt.PersonalFinanceCategory.Detailed
                        : "UNCATEGORIZED";

                    var normalizedKey = key.ToLowerInvariant();
                    if (!categoryCache.TryGetValue(normalizedKey, out var category))
                    {
                        TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
                        category = new TransactionCategory
                        {
                            Primary = primary,
                            Detailed = key,
                            DisplayName = textInfo.ToTitleCase(primary.ToLower()),
                            Description = key.Replace("_", " ")
                        };
                        newCategories.Add(category);
                        categoryCache[key] = category;
                    }

                    var tx = new GimmeTheLoot.Shared.Models.Entity.Transaction
                    {
                        TransactionId = pt.TransactionId,
                        AccountId = account.Id.ToString(),
                        Date = pt.Datetime ?? (pt.Date.HasValue ? pt.Date.Value.ToDateTime(TimeOnly.MinValue) : DateTime.UtcNow),
                        Name = pt.Name,
                        Amount = (decimal)pt.Amount,
                        TransactionCategoryId = category.Id,
                        LogoURL = pt.LogoUrl,
                        MerchantName = pt.MerchantName,
                        UserId = userId
                    };

                    allTransactions.Add(tx);
                }
            }

            // Bulk insert new categories and update cache with their IDs
            await _cacheService.AddNewCategoriesAsync(newCategories);

            // After adding categories, update TransactionCategoryId for new categories
            foreach (var tx in allTransactions.Where(t => t.TransactionCategoryId == 0))
            {
                var normalizedKey = tx.TransactionCategory?.Detailed.ToLowerInvariant() ?? "";
                if (categoryCache.TryGetValue(normalizedKey, out var cat))
                {
                    tx.TransactionCategoryId = cat.Id;
                }
            }

            _db.Transactions.AddRange(allTransactions);
            await _db.SaveChangesAsync();

            return allTransactions;
        }
    }
}
