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
}
