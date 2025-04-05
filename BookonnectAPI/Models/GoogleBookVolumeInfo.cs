using System;
namespace BookonnectAPI.Models;

public class GoogleBookVolumeInfo
{
	public string? Title { get; set; }
	public string[]? Authors { get; set; }
	public string? Description { get; set; }
	public GoogleBookIdentifiers[]? IndustryIdentifiers { get; set; }
    public GoogleBookImageLink? ImageLinks { get; set; }
}

