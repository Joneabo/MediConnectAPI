using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediConnectAPI.Data;
using MediConnectAPI.DTOs;

namespace MediConnectAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly MediConnectContext _db;
    public PatientsController(MediConnectContext db) => _db = db;

    // Solo Admin puede listar todos
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PatientDto>>> GetAll()
    {
        var patients = await _db.Patients
            .Include(p => p.User)
            .Select(p => new PatientDto
            {
                Id = p.Id,
                FullName = p.User.FirstName + " " + p.User.LastName,
                Email = p.User.Email,
                BirthDate = p.BirthDate,
                Gender = p.Gender,
                EmergencyContact = p.EmergencyContact
            })
            .ToListAsync();

        return Ok(patients);
    }

    // Admin o el mismo paciente puede ver sus datos
    [Authorize(Roles = "Admin,Patient")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PatientDto>> GetById(int id)
    {
        var patient = await _db.Patients.Include(p => p.User)
            .Where(p => p.Id == id)
            .Select(p => new PatientDto
            {
                Id = p.Id,
                FullName = p.User.FirstName + " " + p.User.LastName,
                Email = p.User.Email,
                BirthDate = p.BirthDate,
                Gender = p.Gender,
                EmergencyContact = p.EmergencyContact
            })
            .FirstOrDefaultAsync();

        if (patient == null) return NotFound("Paciente no encontrado.");

        // Si es paciente, solo puede ver sus propios datos
        var role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
        var userId = int.Parse(User.Claims.First(c => c.Type == "userId").Value);

        if (role == "Patient")
        {
            var p = await _db.Patients.FirstOrDefaultAsync(x => x.Id == id);
            if (p == null || p.UserId != userId)
                return Forbid("No puedes ver los datos de otro paciente.");
        }

        return Ok(patient);
    }
}
