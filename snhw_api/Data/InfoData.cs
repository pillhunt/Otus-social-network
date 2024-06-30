namespace SocialnetworkHomework.Data
{
    public class InfoData
    {
        public string Message { get; set; } = string.Empty;
    }

    public class UserId
    {
        public Guid Id { get; set; }
    }

    public class RegistrationData
    {
        public string EMail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UserQuestionnaire : UserBaseData
    {
        public string QuestionnaireId { get; set; } = string.Empty;
    }

    public sealed class RequestTask
    {
        public Guid TaskId { get; set; } = Guid.NewGuid();
        public SemaphoreSlim? TaskSemaphore { get; set; }
        public TaskResult Result { get; set; } = new TaskResult();
        public IResult? RequestResult { get; set; }
    }

    public class TaskResult
    {
        public bool IsSuccessful { get; set; }
        public string Result { get; set; }
    }
}
