using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookonnectAPI.Models;

public class AffiliateDetails
{
	public int ID { get; set; }
    public string Link { get; set; } = string.Empty; // affiliate link
    public string SourceID { get; set; } = string.Empty; // affiliate company specific identifier
    [Column(TypeName = "VARCHAR(20)")]
    public string Source { get; set; } = string.Empty; // affiliate company

    /**
     * AffiliateDetails must be associated with a Book
     */
    public int BookID { get; set; }
    public Book Book { get; set; } = null!;
                    
}

