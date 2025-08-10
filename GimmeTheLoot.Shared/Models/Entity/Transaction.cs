using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace GimmeTheLoot.Shared.Models.Entity
{
    public class Transaction
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; }
        [Required]
        public string AccountId { get; set; }  // Plaid AccountId
        public DateTimeOffset Date { get; set; }
        public string? Name { get; set; }       // e.g. "Stables Bar & Grill"
        public decimal Amount { get; set; }
        public string? Category { get; set; }
        public string TransactionId { get; set; } // Plaid transaction id for deduplication
        public string? LogoURL { get; set; }
        public string? MerchantName { get; set; }
        public int? TransactionCategoryId { get; set; }
        public TransactionCategory? TransactionCategory { get; set; }
    }
}
