namespace _301271988_chauhanpachchigar__Lab3.Models
{
    public class Comment
    {
        public string CommentId { get; set; }
        public string MovieId { get; set; }
        public string UserId { get; set; } // Link to user who posted
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
