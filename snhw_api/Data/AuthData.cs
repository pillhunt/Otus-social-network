namespace snhw_api.Data
{

    public class AuthRequestData
    {
        public string EMail { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;   
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponseData
    {
        public Guid UserId { get; set; }

        public string AuthToken { get; set; } = string.Empty;
    }

    public class AuthTokenData
    {
        public string AuthToken { get; set; } = string.Empty;
    }
}
