using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediConnectAPI.Data;
using MediConnectAPI.DTOs;
using MediConnectAPI.Models;

namespace MediConnectAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly MediConnectContext _db;
    public PatientsController(MediConnectContext db) => _db = db;

    // Admin can create/link a Patient profile for a user
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreatePatientRequest req)
    {
        var user = await _db.Users.FindAsync(req.UserId);
        if (user == null) return BadRequest("Usuario no existe.");

        var exists = await _db.Patients.AnyAsync(p => p.UserId == req.UserId);
        if (exists) return BadRequest("El usuario ya tiene perfil de paciente.");

        var patient = new Patient
        {
            UserId = req.UserId,
            BirthDate = req.BirthDate,
            Gender = req.Gender,
            EmergencyContact = req.EmergencyContact
        };

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, new PatientDto
        {
            Id = patient.Id,
            FullName = user.FirstName + " " + user.LastName,
            Email = user.Email,
            BirthDate = patient.BirthDate,
            Gender = patient.Gender,
            EmergencyContact = patient.EmergencyContact
        });
    }

    // Logged-in Patient can fetch their own profile (to learn PatientId)
    [Authorize(Roles = "Patient")]
    [HttpGet("me")]
    public async Task<ActionResult<PatientDto>> GetMe()
    {
        var userId = int.Parse(User.Claims.First(c => c.Type == "userId").Value);
        var patient = await _db.Patients.Include(p => p.User)
            .Where(p => p.UserId == userId)
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

        if (patient == null) return NotFound("Tu usuario no tiene perfil de paciente.");
        return Ok(patient);
    }

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
