using System;
namespace BookonnectAPI.Models;

public class Image
{

    public int ID { get; set; }
	public string Url { get; set; } = String.Empty;
    public string PublicId { get; set; } = String.Empty;
    public string? File { get; set; } = String.Empty;  // base64 file string

    public int BookID { get; set; } // Required foreign key property
    public Book Book { get; set; } = null!; // Required reference navigation. An image must be associated with a book

    public static ImageDTO ImageToDTO(Image image) => new ImageDTO
    {
        ID = image.ID,
        PublicId = image.PublicId,
        Url = image.Url.ToString(),
        BookID = image.BookID
    };

}

