namespace snhw.Data
{
    public class DialogData
    {
        public Guid UserId { get; set; }
        public Guid? DialogId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Status { get; set; } = 0;
        public long StatusTime { get; set; }
    }

    public class DialogDataGet : DialogData
    {
        public List<DialogContactData> Contacts { get; set; } = new List<DialogContactData>();
        public List<DialogMessageData> Messages { get; set; } = new List<DialogMessageData>();
    }

    public class DialogDataSet : DialogData
    {
        public DialogMessageData Message { get; set; } = new DialogMessageData();
        public DialogContactData? Contact { get; set; } = null;
    }

    public class DialogContactData
    {
        public Guid UserId { get; set; }
        public List<DialogMessageData> Messages { get; set; } = new List<DialogMessageData>();
    }

    public class DialogMessageData
    {
        public Guid? Id { get; set; }
        public Guid? ParentId { get; set; }
        public Guid AuthorId { get; set; }
        public int Status { get; set; } = 0;
        public long StatusTime { get; set; }
        public long Created { get; set; }
        public long? Processed { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
