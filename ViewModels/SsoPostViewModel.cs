namespace CIS.ViewModels
{
    public class SsoPostViewModel
    {
        public string TargetUrl { get; set; }
        public Dictionary<string, string> Payload { get; set; }
    }
}