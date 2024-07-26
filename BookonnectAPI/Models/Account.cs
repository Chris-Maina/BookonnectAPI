using System;
namespace BookonnectAPI.Models;

public class Account
{
    public int ID { get; set; }
    public string Provider { get; set; } = String.Empty;
    public int UserID { get; set; }
}

