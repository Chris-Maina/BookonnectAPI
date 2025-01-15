using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BookonnectAPI.Models;

public enum BookCondition
{
    Excellent, // minimal signs of wear, no significant damage, pages are present and tightly bound, No writing, markings, or highlighting, Cover is in excellent condition
    VeryGood, // shows some signs of use, minor bumps to corners or slight fading to the spine, pages have a slight age-toning or a few minor creases, minor markings or highlighting, Cover has minor chipping or fading.
    Good, // shows signs of use, bumps to corners, and some fading to the spine, pages have some age-toning, creases, or occasional light foxing, some writing, markings, or highlighting, but not excessively, Cover has some chipping, fading, or minor tears.
    Fair, // shows significant signs of use and wear, significant shelf wear, damage to corners and spine, and noticeable fading, pages may have significant age-toning, creases, and possible foxing, have extensive writing, markings, or highlighting that might hinder learning, Cover may be significantly damaged
    Poor, // shows significant wear and tear, significant damage to the cover, spine, and pages, Pages may be loose, detached, or missing, have extensive writing, markings, or highlighting, Cover is likely damaged or missing
    Unacceptable // The book is damaged beyond repair and cannot be used for learning purposes, Pages are missing, torn, or significantly damaged, The book may be incomplete or have significant water damage
}

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
    public BookCondition Condition { get; set; } = BookCondition.Good;

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
            Condition = book.Condition,
        };
}