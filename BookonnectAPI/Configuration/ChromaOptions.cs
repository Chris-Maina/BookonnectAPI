using System;
namespace BookonnectAPI.Configuration
{
	public class ChromaOptions
	{
		public const string SectionName = "Authentication:Chroma";
		public string Uri { get; set; } = string.Empty;
    }
}

