using System.Text.Json.Serialization;

namespace snhw.Data
{
    public class UserBaseData
    {
        public string? FirstName { get; set; } = string.Empty;
        public string? SecondName { get; set; } = string.Empty;
        public string? Patronimic { get; set; } = string.Empty;
        public DateTime? Birthday { get; set; }
        public string? PersonalInterest { get; set; } = string.Empty;
        public string? City { get; set; } = string.Empty;
        public Gender? Gender { get; set; }
    }
    public abstract class UserCommonData : UserBaseData
    {        
        public string Email { get; set; } = string.Empty;        
    }

    public class UserEditData : UserCommonData;

    public class UserInfo : UserCommonData
    {
        public Guid Id { get; set; }
        public int Status { get; set; }
    }

    public class ContactData
    {
        public Guid Id { get; set; }
        public int Status { get; set; }
        public string Comment { get; set; } = string.Empty ;
    }

    /// <summary>
    /// Пол
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Gender
    {
        /// <summary>
        /// Other
        /// </summary>
        Other,
        /// <summary>
        /// Male
        /// </summary>
        Male,
        /// <summary>
        /// Female
        /// </summary>
        Female
    }
}
