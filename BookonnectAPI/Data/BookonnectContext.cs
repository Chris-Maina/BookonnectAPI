using BookonnectAPI.Configuration;
using BookonnectAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BookonnectAPI.Data;

public class BookonnectContext: DbContext
{
    private MailSettingsOptions _mailSettings;
    public BookonnectContext(DbContextOptions<BookonnectContext> options, IOptionsSnapshot<MailSettingsOptions> mailSettings)
		: base(options)
	{
        _mailSettings = mailSettings.Value;
	}

    public DbSet<User> Users { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Book> Books { get; set; }
    public DbSet<Image> Images { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Payment> Payments { get; set; }
	public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Confirmation> Confirmations { get; set; }
    public DbSet<InventoryLog> InventoryLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasData(
            new User { ID = 1, Name = _mailSettings.Name, Email = _mailSettings.EmailId, Image = _mailSettings.Picture, Phone = "" });

        modelBuilder.ApplyConfiguration(new InventoryLogConfiguration());
    }
}

