using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediConnectAPI.Data;
using MediConnectAPI.DTOs;
using MediConnectAPI.Models;

namespace MediConnectAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DoctorsController : ControllerBase
{
    private readonly MediConnectContext _db;
    public DoctorsController(MediConnectContext db) => _db = db;

    // ✅ Solo Admin puede ver todos los doctores
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DoctorDto>>> GetAll()
    {
        var doctors = await _db.Doctors
            .Include(d => d.User)
            .Include(d => d.Specialty)
            .Select(d => new DoctorDto
            {
                Id = d.Id,
                FullName = d.User.FirstName + " " + d.User.LastName,
                Email = d.User.Email,
                LicenseNumber = d.LicenseNumber,
                Specialty = d.Specialty.Name
            })
            .ToListAsync();

        return Ok(doctors);
    }

    // ✅ Admin o el mismo doctor pueden ver perfil
    [Authorize(Roles = "Admin,Doctor")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<DoctorDto>> GetById(int id)
    {
        var doctor = await _db.Doctors
            .Include(d => d.User)
            .Include(d => d.Specialty)
            .Where(d => d.Id == id)
            .Select(d => new DoctorDto
            {
                Id = d.Id,
                FullName = d.User.FirstName + " " + d.User.LastName,
                Email = d.User.Email,
                LicenseNumber = d.LicenseNumber,
                Specialty = d.Specialty.Name
            })
            .FirstOrDefaultAsync();

        if (doctor == null) return NotFound("Doctor no encontrado.");

        var role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
        var userId = int.Parse(User.Claims.First(c => c.Type == "userId").Value);

        if (role == "Doctor")
        {
            var d = await _db.Doctors.FirstOrDefaultAsync(x => x.Id == id);
            if (d == null || d.UserId != userId)
                return Forbid("No puedes ver el perfil de otro doctor.");
        }

        return Ok(doctor);
    }

    // ✅ Solo Admin puede crear doctor
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] Doctor d)
    {
        _db.Doctors.Add(d);
        await _db.SaveChangesAsync();
        return Ok("Doctor registrado correctamente.");
    }

    // ✅ Admin o el mismo doctor pueden actualizar
    [Authorize(Roles = "Admin,Doctor")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] Doctor d)
    {
        if (id != d.Id) return BadRequest("Id no coincide.");

        var role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
        var userId = int.Parse(User.Claims.First(c => c.Type == "userId").Value);

        if (role == "Doctor")
        {
            var doctor = await _db.Doctors.FirstOrDefaultAsync(x => x.Id == id);
            if (doctor == null || doctor.UserId != userId)
                return Forbid("No puedes modificar otro perfil.");
        }

        _db.Entry(d).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return Ok("Doctor actualizado correctamente.");
    }

    // ✅ Solo Admin puede eliminar doctor
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var doctor = await _db.Doctors.FindAsync(id);
        if (doctor == null) return NotFound("Doctor no encontrado.");

        _db.Doctors.Remove(doctor);
        await _db.SaveChangesAsync();
        return Ok("Doctor eliminado correctamente.");
    }
}
