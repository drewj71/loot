namespace TheLoot.Data
{
    public class UserPlaidAccount
    {
        public int Id { get; set; }
        public string UserId { get; set; } // link to your app user
        public string AccessToken { get; set; }
        public string ItemId { get; set; }
        public DateTime ConnectedOn { get; set; }
        // Add refresh tokens, expiry if needed
    }
}
