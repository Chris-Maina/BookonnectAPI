using BookonnectAPI.Models;
namespace BookonnectAPI.DTO;

public class AffiliateDetailsDTO
{
    public int ID { get; set; }
    public string? Link { get; set; }
    public string? SourceID { get; set; }
    public string? Source { get; set; }
    public int BookID { get; set; }
    public BookDTO? Book { get; set; }
}

