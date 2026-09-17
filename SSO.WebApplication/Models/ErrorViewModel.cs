namespace SSO.WebApplication.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public int? StatusCode { get; set; }
    public string Title { get; set; } = "Something Went Wrong";
    public string Message { get; set; } = "We couldn't complete your request right now.";
    public string ActionText { get; set; } = "Back to Sign In";
    public string ActionUrl { get; set; } = "/Login";

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
