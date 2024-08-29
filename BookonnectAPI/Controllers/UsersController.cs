using BookonnectAPI.Data;
using BookonnectAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookonnectAPI.Controllers;

[ApiController]
[Authorize]
[Route("/api/[controller]")]
public class UsersController: ControllerBase
{
	private readonly BookonnectContext _context;
	public UsersController(BookonnectContext context)
	{
		_context = context;
        _context.Database.EnsureCreated();
    }


	[HttpPost]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<User>> PostUser(User user)
	{
		bool userExists = _context.Users.Any(u => u.Email == user.Email);
		if (userExists)
		{
			return Conflict(new { Message = "User already exisits" });

        }
		_context.Users.Add(user);
        try
        {
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUser), new { id = user.ID }, user);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
		
	}

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<User[]>> GetUsers()
    {
        var users = await _context.Users.ToListAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<User>> GetUser(int id)
	{
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        return Ok(user);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> PutUser(int id, [FromBody] User user)
    {
        if (id != user.ID)
        {
            return BadRequest(new { Message = "ID in params does not match user id in payload. Check and try again" });
        }

        if (!UserExists(id))
        {
            return NotFound(new { Message = "User not found" });
        }

        _context.Users.Entry(user).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
            return NoContent();

        } catch (DbUpdateConcurrencyException ex)
        {
            if (!UserExists(id))
            {
                return NotFound(new { Message = "User not found" });
            }
            else
            {
                // log exception
                return StatusCode(500, ex.Message);
            }
        } catch (Exception ex)
        {
            // log exception
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<User>> PatchUser(int id, [FromBody] JsonPatchDocument<User> patchDocument)
    {
        if (patchDocument == null)
        {
            return BadRequest(ModelState);
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        patchDocument.ApplyTo(user, ModelState);
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _context.Update(user);
        try
        {
            await _context.SaveChangesAsync();
            return Ok(user);
        } catch (DbUpdateConcurrencyException ex)
        {
            if (!UserExists(id))
            {
                return NotFound(new { Message = "User not found" });
            }
            else
            {
                return StatusCode(500, ex.Message);
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }

    }

    private bool UserExists(int id) => _context.Users.Any(user => user.ID == id);
}

