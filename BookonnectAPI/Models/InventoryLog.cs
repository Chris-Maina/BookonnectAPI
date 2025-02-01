namespace BookonnectAPI.Models;

public enum ChangeType
{
    InitialStock,
    Adjustment,
    Deletion,
    Sale
};

public class InventoryLog
{
	public int ID { get; set; }
	public int Quantity { get; set; }
    public DateTime DateTime { get; set; }
    public ChangeType Type { get; set; }
    public int BookID { get; set; } // Required foreign key property.
    public Book? Book { get; set; } // Optional reference navigation
    
}

