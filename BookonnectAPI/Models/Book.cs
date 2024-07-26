﻿namespace BookonnectAPI.Models;

public class Book
{
    public int ID { get; set; }
    public string Title { get; set; } = String.Empty;
    public string Author { get; set; } = String.Empty;
    public string ISBN { get; set; } = string.Empty;
    public float Price { get; set; }
    public string? Description { get; set; }

    public int UserID { get; set; } // Required foreign key property. Indicates the owner of the book
    public User User { get; set; } = null!; // Required reference navigation. A book cannot exist without an owner

    public int? OrderId { get; set; } // Optional foreign key property
    public Order? Order { get;  }  // Optional reference navigation. A book does not need to be associated with an Order

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
            Image = book.Image == null ? null : new ImageDTO
            {
                ID = book.Image.ID,
                PublicId = book.Image.PublicId,
                Url = book.Image.Url,
            }
        };
}