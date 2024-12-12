using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookonnectAPI.Models;

public class Account
{
    public int ID { get; set; }
    [Column(TypeName = "VARCHAR(20)")]
    public string Provider { get; set; } = String.Empty;
    public int UserID { get; set; }
}

