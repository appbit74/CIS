namespace CIS.ViewModels
{
    public class AdUserViewModel
    {
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public bool IsLockedOut { get; set; }
    }
}