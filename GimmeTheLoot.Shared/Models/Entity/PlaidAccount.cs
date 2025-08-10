using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace GimmeTheLoot.Shared.Models.Entity
{
    public class PlaidAccount
    {
        [Key]
        public int Id { get; set; }
        public string AccountId { get; set; }  // Plaid Account ID
        public string Name { get; set; }
        public string OfficialName { get; set; }
        public string Type { get; set; }       // e.g. "credit", "depository"
        public string Subtype { get; set; }    // e.g. "checking", "credit card"
        public decimal CurrentBalance { get; set; }
        public decimal AvailableBalance { get; set; }


        [ForeignKey("UserPlaidAccount")]
        public int UserPlaidAccountId { get; set; } = default!;
        [JsonIgnore]
        public UserPlaidAccount UserPlaidAccount { get; set; } = default!;
    }
}
