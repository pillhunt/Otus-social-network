namespace SocialnetworkHomework.Data
{
    public class PostData
    {
        public Guid Id { get; set; }
    }

    public class PostEditData : PostData
    {
        public string Text { get; set; } = string.Empty;
        public int Status {  get; set; }
        public DateTime Processed { get; set; }
        public DateTime Created { get; set; }
    }

    public class PostGetData : PostEditData;
}
