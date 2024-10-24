namespace snhw.Data
{
    public class PostData
    {
        public Guid Id { get; set; }
    }

    public class PostEditData : PostData
    {
        public string Text { get; set; } = string.Empty;
        public int Status { get; set; } = 0;
        public long? Processed { get; set; } = 0;
        public long Created { get; set; } = 0;
    }

    public class PostGetData : PostEditData;

    public class FeedPostData : PostGetData
    {
        public Guid AuthorId { get; set; }
    }
}
