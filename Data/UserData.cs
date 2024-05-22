namespace SocialnetworkHomework.Data
{
    public abstract class UserCommonData
    {
        public string FirstName { get; set; } = string.Empty;
        public string SecondName { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public string PersonalInterest { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public Gender Gender { get; set; }
        public int Status { get; set; }
    }


    public class UserInfo : UserCommonData
    {
        public Guid Id { get; set; }
    }

    /// <summary>
    /// Пол
    /// </summary>
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
