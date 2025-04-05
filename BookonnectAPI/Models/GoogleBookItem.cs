using System;
namespace BookonnectAPI.Models;

public class GoogleBookItem
{
    public string Id { get; set; } = string.Empty;
    public GoogleBookVolumeInfo? VolumeInfo { get; set; }
}

