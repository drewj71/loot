using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
namespace GimmeTheLoot.Shared.Models.Entity
{
    public class TransactionCategory
    {
        [Key]
        public int Id { get; set; }
        public string Primary { get; set; }
        public string Detailed { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        [NotMapped]
        public string? CustomDisplay
        {
            get
            {
                if (string.IsNullOrEmpty(Primary)) return String.Empty;

                var replaced = Primary.Replace('_', ' ').ToLowerInvariant();
                TextInfo textinfo = CultureInfo.CurrentCulture.TextInfo;
                return textinfo.ToTitleCase(replaced);
            }
        }
    }
}
