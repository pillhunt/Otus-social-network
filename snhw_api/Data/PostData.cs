namespace snhw.Data
{
    public class PostData
    {
        public Guid Id { get; set; }
    }

    public class PostEditData : PostData
    {
        public string Text { get; set; } = string.Empty;
        public int Status {  get; set; }
        public long Processed { get; set; }
        public long Created { get; set; }
    }

    public class PostGetData : PostEditData;

    public class FeedPostData : PostGetData
    {
        public Guid AuthorId { get; set; }
    }
}
