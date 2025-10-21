using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediConnectAPI.Data;
using MediConnectAPI.DTOs;
using MediConnectAPI.Models;

namespace MediConnectAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClinicalHistoryController : ControllerBase
{
    private readonly MediConnectContext _db;
    public ClinicalHistoryController(MediConnectContext db) => _db = db;

    // ✅ Obtener historial clínico de un expediente
    [Authorize(Roles = "Admin,Doctor,Patient")]
    [HttpGet("record/{recordId:int}")]
    public async Task<ActionResult<IEnumerable<ClinicalHistoryDto>>> GetByRecord(int recordId)
    {
        var role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
        var userId = int.Parse(User.Claims.First(c => c.Type == "userId").Value);

        // Paciente: solo puede ver su propio expediente
        if (role == "Patient")
        {
            var patient = await _db.Patients
                .Include(p => p.MedicalRecord)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (patient == null || patient.MedicalRecord == null || patient.MedicalRecord.Id != recordId)
            {
                return Forbid("No puedes ver historiales clínicos de otro paciente.");
            }
        }

        // Admin y Doctor pueden ver cualquier historial
        var histories = await _db.ClinicalHistories
            .Where(c => c.MedicalRecordId == recordId)
            .Include(c => c.Appointment)
            .Select(c => new ClinicalHistoryDto
            {
                Id = c.Id,
                AppointmentId = c.AppointmentId,
                RegisteredAt = c.RegisteredAt,
                Diagnosis = c.Diagnosis,
                Treatment = c.Treatment,
                Reason = c.Reason,
                Notes = c.Notes
            })
            .ToListAsync();

        return Ok(histories);
    }

    // ✅ Obtener un registro específico
    [Authorize(Roles = "Admin,Doctor,Patient")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClinicalHistoryDto>> GetById(int id)
    {
        var history = await _db.ClinicalHistories
            .Include(c => c.MedicalRecord).ThenInclude(m => m.Patient)
            .Include(c => c.Appointment)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (history == null) return NotFound("Registro clínico no encontrado.");

        var role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
        var userId = int.Parse(User.Claims.First(c => c.Type == "userId").Value);

        // Paciente: solo su propio historial
        if (role == "Patient" && history.MedicalRecord.Patient.UserId != userId)
        {
            return Forbid("No puedes ver registros clínicos de otro paciente.");
        }

        // Admin y Doctor → pueden ver cualquier historial
        return Ok(new ClinicalHistoryDto
        {
            Id = history.Id,
            AppointmentId = history.AppointmentId,
            RegisteredAt = history.RegisteredAt,
            Diagnosis = history.Diagnosis,
            Treatment = history.Treatment,
            Reason = history.Reason,
            Notes = history.Notes
        });
    }

    // ✅ Crear registro clínico (solo Doctor o Admin)
    [Authorize(Roles = "Admin,Doctor")]
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] ClinicalHistory history)
    {
        history.RegisteredAt = DateTime.UtcNow;

        _db.ClinicalHistories.Add(history);
        await _db.SaveChangesAsync();
        return Ok("Registro clínico creado correctamente.");
    }

    // ✅ Actualizar (solo Doctor o Admin)
    [Authorize(Roles = "Admin,Doctor")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] ClinicalHistory history)
    {
        if (id != history.Id) return BadRequest("Id no coincide.");
        _db.Entry(history).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return Ok("Registro clínico actualizado correctamente.");
    }

    // ✅ Eliminar (solo Admin)
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var history = await _db.ClinicalHistories.FindAsync(id);
        if (history == null) return NotFound("Registro clínico no encontrado.");

        _db.ClinicalHistories.Remove(history);
        await _db.SaveChangesAsync();
        return Ok("Registro clínico eliminado correctamente.");
    }
}
