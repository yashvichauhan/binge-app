namespace _301271988_chauhanpachchigar__Lab3.Models
{
    public class Movie
    {
        public string MovieId { get; set; }
        public string Title { get; set; }
        public string Genre { get; set; }
        public string Director { get; set; }
        public DateTime ReleaseTime { get; set; }
        public string UploadedByUserId { get; set; } 
        public double Rating { get; set; }
        public string S3Url { get; set; }
    }
}
