using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediConnectAPI.Data;
using MediConnectAPI.Models;

namespace MediConnectAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AppointmentStatusesController : ControllerBase
{
    private readonly MediConnectContext _db;
    public AppointmentStatusesController(MediConnectContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppointmentStatus>>> GetAll()
    {
        var list = await _db.AppointmentStatuses.AsNoTracking().ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppointmentStatus>> GetById(int id)
    {
        var status = await _db.AppointmentStatuses.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        return status is null ? NotFound("Estado no encontrado.") : Ok(status);
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] AppointmentStatus status)
    {
        if (string.IsNullOrWhiteSpace(status.Name))
            return BadRequest("El nombre es obligatorio.");

        _db.AppointmentStatuses.Add(status);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = status.Id }, status);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] AppointmentStatus status)
    {
        if (id != status.Id)
            return BadRequest("Id no coincide.");

        var exists = await _db.AppointmentStatuses.AnyAsync(s => s.Id == id);
        if (!exists)
            return NotFound("Estado no encontrado.");

        _db.Entry(status).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return Ok("Estado actualizado correctamente.");
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var status = await _db.AppointmentStatuses.Include(s => s.Appointments).FirstOrDefaultAsync(s => s.Id == id);
        if (status == null)
            return NotFound("Estado no encontrado.");

        if (status.Appointments.Any())
            return BadRequest("No se puede eliminar un estado que está siendo usado por citas.");

        _db.AppointmentStatuses.Remove(status);
        await _db.SaveChangesAsync();
        return Ok("Estado eliminado correctamente.");
    }
}

