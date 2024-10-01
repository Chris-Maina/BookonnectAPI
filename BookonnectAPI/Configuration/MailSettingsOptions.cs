namespace BookonnectAPI.Configuration;

public class MailSettingsOptions
{
    public const string SectionName = "MailSettings";
    public string Host { get; set; } = string.Empty;
	public int Port { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EmailId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseSSL { get; set; }
}

