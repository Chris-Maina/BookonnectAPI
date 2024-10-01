using System;
namespace BookonnectAPI.Models;

public class Email
{
	public string ToId { get; set; } = String.Empty;
	public string Name { get; set; } = String.Empty;
    public string Subject { get; set; } = String.Empty;
    public string Body { get; set; } = String.Empty;
}

