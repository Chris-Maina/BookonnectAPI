using BookonnectAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BookonnectAPI.Data;

public class BookonnectContext: DbContext
{
	public BookonnectContext(DbContextOptions<BookonnectContext> options)
		: base(options)
	{
	}

	public DbSet<User> Users { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Book> Books { get; set; }
    public DbSet<Image> Images { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Payment> Payments { get; set; }
	public DbSet<Delivery> Deliveries { get; set; }
	public DbSet<CartItem> CartItems { get; set; }
}

