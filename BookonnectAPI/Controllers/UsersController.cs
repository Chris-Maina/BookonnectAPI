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
	public async Task<ActionResult<User>> PostUser(User user)
	{
		bool userExists = _context.Users.Any(u => u.Email == user.Email);
		if (userExists)
		{
			return Conflict();

        }
		_context.Users.Add(user);
        try
        {
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(PostUser), new { id = user.ID }, user);
        }
        catch (Exception)
        {
            throw;
        }
		
	}

    [HttpGet]
    public async Task<ActionResult<User[]>> GetUsers()
    {
        var users = await _context.Users.ToListAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
	public async Task<ActionResult<User>> GetUser(int id)
	{
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> PutUser(int id, [FromBody] User user)
    {
        if (id != user.ID)
        {
            return BadRequest();
        }

        if (!UserExists(id))
        {
            return NotFound();
        }

        _context.Users.Entry(user).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
            return NoContent();

        } catch (DbUpdateConcurrencyException)
        {
            if (!UserExists(id))
            {
                return NotFound();
            }
            else
            {
                // log exception
                throw;
            }
        } catch (Exception)
        {
            // log exception
            throw;
        }
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<User>> PatchUser(int id, [FromBody] JsonPatchDocument<User> patchDocument)
    {
        if (patchDocument == null)
        {
            return BadRequest(ModelState);
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
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
            return new ObjectResult(user);
        } catch (DbUpdateConcurrencyException)
        {
            if (!UserExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }
        catch (Exception)
        {
            throw;
        }

    }

    private bool UserExists(int id) => _context.Users.Any(user => user.ID == id);
}

