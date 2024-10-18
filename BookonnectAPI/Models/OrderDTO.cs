﻿using System.Text.Json.Serialization;

namespace BookonnectAPI.Models;

public class OrderDTO
{
	public int ID { get; set; }
	public float Total { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OrderStatus Status { get; set; }
    public ICollection<OrderItemDTO> OrderItems { get; set; } = new List<OrderItemDTO>();
    public int UserID { get; set; }
    public User? User { get; set; }
    public Delivery? Delivery { get; set; }
}

