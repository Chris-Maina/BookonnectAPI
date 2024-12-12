using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BookonnectAPI.Models;

public class Book
{
    public int ID { get; set; }
    [Column(TypeName = "VARCHAR(50)")]
    public string Title { get; set; } = String.Empty;
    [Column(TypeName = "VARCHAR(50)")]
    public string Author { get; set; } = String.Empty;
    [Column(TypeName = "VARCHAR(20)")]
    public string ISBN { get; set; } = string.Empty;
    public float Price { get; set; }
    public string? Description { get; set; }
    public bool Visible { get; set; } = true;

    public int VendorID { get; set; } // Required foreign key property. Indicates the owner/vendor of the book
    [JsonIgnore]
    public User Vendor { get; set; } = null!; // Required reference navigation. A book cannot exist without an owner

    public Image? Image { get; set; } // Optional reference navigation. A book exist without an image.

    public static BookDTO BookToDTO(Book book) =>
        new BookDTO
        {
            ID = book.ID,
            Title = book.Title,
            Author = book.Author,
            ISBN = book.ISBN,
            Price = book.Price,
            Description = book.Description,
            VendorID = book.VendorID,
            Vendor = User.UserToDTO(book.Vendor),
            Visible = book.Visible,
            Image = book.Image != null ? Image.ImageToDTO(book.Image) : null,
        };
}