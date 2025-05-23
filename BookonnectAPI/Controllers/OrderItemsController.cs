﻿using System.Security.Claims;
using BookonnectAPI.Data;
using BookonnectAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace BookonnectAPI.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Policy = "UserClaimPolicy")]
[ApiController]
[ApiVersion("1.0")]
public class OrderItemsController : ControllerBase
{
    private readonly BookonnectContext _context;
    private readonly ILogger<OrderItem> _logger;

    public OrderItemsController(BookonnectContext context, ILogger<OrderItem> logger)
    {
        _context = context;
        _logger = logger;

    }

    // GET: api/orderitems
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderItemDTO[]>> GetOrderItems([FromQuery] OrderItemQueryParameters orderItemQueryParameters)
    {
        _logger.LogInformation("Fetching order items");
        var userID = this.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userID == null)
        {
            _logger.LogWarning("No token found");
            return Unauthorized(new { Message = "Please sign in again." });
        }

        OrderItemDTO[] orderItems;
        if (orderItemQueryParameters.Role == "customer")
        {
            _logger.LogInformation("Fetching order items for logged in customer");
            orderItems = await _context.OrderItems
                .OrderBy(ordItem => ordItem.ID)
                .Where(ordItem => ordItem.Order != null && ordItem.Order.CustomerID == int.Parse(userID))
                .Include(orderItem => orderItem.Order)
                .Include(ordItem => ordItem.Confirmations)
                .Include(ordItem => ordItem.Book)
                .ThenInclude(bk => bk != null ? bk.Image : null)
                .Include(ordItem => ordItem.Book)
                .ThenInclude(bk => bk != null ? bk.OwnedDetails : null)
                .ThenInclude(od => od != null ? od.Vendor : null)
                .Select(ordItem => OrderItem.OrderItemToDTO(ordItem))
                .ToArrayAsync();

            return Ok(orderItems);
        };

        _logger.LogInformation("Fetching order items for logged in vendor");
        orderItems = await _context.OrderItems
            .OrderBy(ordItem => ordItem.ID)
            .Where(ordItem => ordItem.Book != null && ordItem.Book.OwnedDetails != null && ordItem.Book.OwnedDetails.VendorID == int.Parse(userID))
            .Include(orderItem => orderItem.Order)
            .Include(ordItem => ordItem.Confirmations)
            .Include(ordItem => ordItem.Book)
            .ThenInclude(bk => bk != null ? bk.Image : null)
            .Include(ordItem => ordItem.Book)
            .ThenInclude(bk => bk != null ? bk.OwnedDetails : null)
            .ThenInclude(od => od != null ? od.Vendor : null)
            .Select(ordItem => OrderItem.OrderItemToDTO(ordItem))
            .ToArrayAsync();


        return Ok(orderItems);
    }

    // GET api/orderitems/5
    [HttpGet("{id}")]
    public string Get(int id)
    {
        return "value";
    }
}

