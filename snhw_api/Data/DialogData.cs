namespace snhw.Data
{
    public class DialogData
    {
        public Guid? DialogId { get; set; }
        public string DialogName { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public Guid ContactId { get; set; }
        public string MessageText { get; set; } = string.Empty;
    }

    public class DialogDataEdit : DialogData
    {
        public Guid? MessageId { get; set; }
        public Guid? MessageParentId { get; set; }
        public int StatusByUser { get; set; }
    }

    public class DialogDataGet : DialogDataEdit 
    { 
        public long StatusByUserTime { get; set; }
        public long StatusByContactTime { get; set; }
        public long MessageCreated { get; set; }
        public long? MessageProcessed { get ; set; }
    }
}
