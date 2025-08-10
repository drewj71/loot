using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace GimmeTheLoot.Shared.Models.Entity
{
    public class UserPlaidAccount
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; } // my apps user id
        [Required]
        public string AccessToken { get; set; }
        public string ItemId { get; set; }
        public DateTime ConnectedOn { get; set; }
        [JsonIgnore]
        public List<PlaidAccount> Accounts { get; set; }
    }
}
