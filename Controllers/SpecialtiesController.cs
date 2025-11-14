using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediConnectAPI.Data;
using MediConnectAPI.Models;

namespace MediConnectAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class SpecialtiesController : ControllerBase
{
    private readonly MediConnectContext _db;
    public SpecialtiesController(MediConnectContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DoctorSpecialty>>> GetAll()
    {
        var list = await _db.Specialties.AsNoTracking().ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DoctorSpecialty>> GetById(int id)
    {
        var specialty = await _db.Specialties.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        return specialty is null ? NotFound("Especialidad no encontrada.") : Ok(specialty);
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] DoctorSpecialty specialty)
    {
        if (string.IsNullOrWhiteSpace(specialty.Name))
            return BadRequest("El nombre es obligatorio.");

        _db.Specialties.Add(specialty);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = specialty.Id }, specialty);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] DoctorSpecialty specialty)
    {
        if (id != specialty.Id)
            return BadRequest("Id no coincide.");

        var exists = await _db.Specialties.AnyAsync(s => s.Id == id);
        if (!exists)
            return NotFound("Especialidad no encontrada.");

        _db.Entry(specialty).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return Ok("Especialidad actualizada correctamente.");
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var specialty = await _db.Specialties.Include(s => s.Doctors).FirstOrDefaultAsync(s => s.Id == id);
        if (specialty == null)
            return NotFound("Especialidad no encontrada.");

        if (specialty.Doctors.Any())
            return BadRequest("No se puede eliminar una especialidad con doctores asignados.");

        _db.Specialties.Remove(specialty);
        await _db.SaveChangesAsync();
        return Ok("Especialidad eliminada correctamente.");
    }
}

