using System;
namespace BookonnectAPI.Models;

public class ReviewDTO
{
    public int ID { get; set; }
    public ReviewStatus Status { get; set; }
    public DateTime DateTime { get; set; }
    public string? Text { get; set; }
    public int BookID { get; set; }
    public BookDTO? Book { get; set; }
}

